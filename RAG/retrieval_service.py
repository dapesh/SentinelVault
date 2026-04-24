import asyncio
from typing import List, Dict, Any
from pydantic import BaseModel, Field, ValidationError
from ai_client import get_openai_client, OPENAI_EMBEDDING_MODEL
import os
from neo4j import AsyncGraphDatabase
from qdrant_client import AsyncQdrantClient
from dotenv import load_dotenv

load_dotenv(override=True)

# Pydantic models for strict shape validation
class VectorSearchResult(BaseModel):
    id: str
    content: str
    score: float = Field(description="Cosine similarity or distance score")
    metadata: Dict[str, Any]

class GraphSearchResult(BaseModel):
    node_id: str
    content: str
    relevance_score: float = Field(description="Graph traversal relevance score")
    context: str

class RRFMergeError(Exception):
    """Exception raised when RRF merger encounters incompatible data shapes."""
    pass

class HybridRetrievalService:
    def __init__(self):
        self.openai_client = get_openai_client()
        
        # Qdrant client
        qdrant_url = os.getenv("QDRANT_URL", "http://localhost:6333")
        self.qdrant_client = AsyncQdrantClient(url=qdrant_url)
        self.collection_name = "securevault"
        
        # Neo4j client
        neo4j_uri = os.getenv("NEO4J_URI", "bolt://localhost:7687")
        neo4j_user = os.getenv("NEO4J_USER", "neo4j")
        neo4j_password = os.getenv("NEO4J_PASSWORD", "securevault_password")
        self.neo4j_driver = AsyncGraphDatabase.driver(neo4j_uri, auth=(neo4j_user, neo4j_password))

    async def get_embedding(self, query: str) -> List[float]:
        response = await self.openai_client.embeddings.create(
            input=query,
            model=OPENAI_EMBEDDING_MODEL
        )
        return response.data[0].embedding

    async def fetch_from_qdrant(self, embedding: List[float], query: str) -> List[VectorSearchResult]:
        try:
            results = await self.qdrant_client.search(
                collection_name=self.collection_name,
                query_vector=embedding,
                limit=5
            )
            parsed_results = []
            for hit in results:
                payload = hit.payload or {}
                parsed_results.append(VectorSearchResult(
                    id=str(hit.id),
                    content=payload.get("content", ""),
                    score=hit.score,
                    metadata=payload
                ))
            return parsed_results
        except Exception as e:
            print(f"Qdrant fetch error: {e}")
            return []

    async def _execute_cypher_search(self, tx, query: str):
        words = [w for w in query.split() if len(w) > 3]
        if not words:
            return []
            
        cypher_q = """
        MATCH (s)-[r]->(o)
        WHERE any(w IN $words WHERE toLower(s.name) CONTAINS toLower(w) OR toLower(o.name) CONTAINS toLower(w))
        RETURN s.name as subject, type(r) as predicate, o.name as object, r.context as context
        LIMIT 10
        """
        result = await tx.run(cypher_q, words=words)
        return await result.values()

    async def fetch_from_neo4j(self, query: str) -> List[GraphSearchResult]:
        try:
            async with self.neo4j_driver.session() as session:
                records = await session.execute_read(self._execute_cypher_search, query)
                
            parsed_results = []
            for i, record in enumerate(records):
                subject, predicate, obj, context = record
                parsed_results.append(GraphSearchResult(
                    node_id=f"neo4j_{subject}_{i}",
                    content=f"{subject} {predicate} {obj}",
                    relevance_score=1.0, # Basic match weight
                    context=context or "No context"
                ))
            return parsed_results
        except Exception as e:
            print(f"Neo4j fetch error: {e}")
            return []

    def reciprocal_rank_fusion(self, vector_results: List[VectorSearchResult], graph_results: List[GraphSearchResult], k: int = 60) -> List[Dict[str, Any]]:
        """
        Implements Reciprocal Rank Fusion to merge vector and graph results.
        Formula: S(d) = sum(1 / (k + r(d)))
        """
        rrf_scores: Dict[str, float] = {}
        doc_metadata: Dict[str, Dict[str, Any]] = {}

        # Helper to process result sets
        def process_results(results, source_name, id_attr, score_attr):
            # Sort only if not already sorted by your DB driver
            sorted_res = sorted(results, key=lambda x: getattr(x, score_attr), reverse=True)
            
            for rank, res in enumerate(sorted_res, start=1):
                doc_id = getattr(res, id_attr)
                
                # Calculate RRF
                rrf_scores[doc_id] = rrf_scores.get(doc_id, 0.0) + (1.0 / (k + rank))
                
                # Update Metadata
                if doc_id not in doc_metadata:
                    doc_metadata[doc_id] = {
                        "id": doc_id,
                        "content": res.content,
                        "source": source_name,
                        "context": getattr(res, 'context', None)
                    }
                else:
                    doc_metadata[doc_id]["source"] = "hybrid"
                    if hasattr(res, 'context'):
                        doc_metadata[doc_id]["context"] = res.context

        process_results(vector_results, "vector", "id", "score")
        process_results(graph_results, "graph", "node_id", "relevance_score")

        # Final Sort
        final_results = []
        for doc_id, score in sorted(rrf_scores.items(), key=lambda x: x[1], reverse=True):
            doc = doc_metadata[doc_id]
            doc["rrf_score"] = score
            final_results.append(doc)
            
        return final_results

    async def query(self, query_string: str) -> List[Dict[str, Any]]:
        """
        Orchestrates the hybrid retrieval flow.
        """
        # 1. Get embedding
        embedding = await self.get_embedding(query_string)
        
        # 2. Concurrent fetches
        vector_results, graph_results = await asyncio.gather(
            self.fetch_from_qdrant(embedding, query_string),
            self.fetch_from_neo4j(query_string)
        )
        
        # 3. RRF Merge
        merged_results = self.reciprocal_rank_fusion(vector_results, graph_results)
        
        await self.neo4j_driver.close()
        
        return merged_results
