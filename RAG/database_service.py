import os
import uuid
import asyncio
from typing import List, Dict, Any
from dotenv import load_dotenv

from neo4j import AsyncGraphDatabase
from qdrant_client import AsyncQdrantClient
from qdrant_client.http.models import Distance, VectorParams, PointStruct
from ai_client import get_openai_client, OPENAI_EMBEDDING_MODEL

load_dotenv(override=True)

class DatabaseService:
    def __init__(self):
        # Neo4j Setup
        neo4j_uri = os.getenv("NEO4J_URI", "bolt://localhost:7687")
        neo4j_user = os.getenv("NEO4J_USER", "neo4j")
        neo4j_password = os.getenv("NEO4J_PASSWORD", "securevault_password")
        self.neo4j_driver = AsyncGraphDatabase.driver(neo4j_uri, auth=(neo4j_user, neo4j_password))
        
        # Qdrant Setup
        qdrant_url = os.getenv("QDRANT_URL", "http://localhost:6333")
        self.qdrant_client = AsyncQdrantClient(url=qdrant_url)
        self.collection_name = "securevault"
        
        # OpenAI Setup for generating embeddings before ingestion
        self.openai_client = get_openai_client()
        self.vector_size = 1536 # For text-embedding-3-small

    async def initialize(self):
        """Ensure collections exist"""
        try:
            # Check if Qdrant collection exists
            exists = await self.qdrant_client.collection_exists(self.collection_name)
            if not exists:
                await self.qdrant_client.create_collection(
                    collection_name=self.collection_name,
                    vectors_config=VectorParams(size=self.vector_size, distance=Distance.COSINE)
                )
        except Exception as e:
            print(f"Failed to initialize Qdrant collection: {e}")

    async def close(self):
        await self.neo4j_driver.close()

    async def _execute_cypher_transaction(self, tx, queries: List[str]):
        for query in queries:
            await tx.run(query)

    async def push_to_neo4j(self, cypher_queries: List[str]):
        """Executes a list of Cypher queries within a transaction"""
        if not cypher_queries:
            return
            
        async with self.neo4j_driver.session() as session:
            await session.execute_write(self._execute_cypher_transaction, cypher_queries)

    async def push_to_qdrant(self, vector_docs: List[Dict[str, Any]]):
        """Generates embeddings and pushes vectors to Qdrant"""
        if not vector_docs:
            return
            
        points = []
        # Batch embedding generation could be more efficient, but we will do it sequentially for simplicity and stability here
        for doc in vector_docs:
            content = doc["content"]
            metadata = doc["metadata"]
            
            try:
                # Generate embedding
                response = await self.openai_client.embeddings.create(
                    input=content,
                    model=OPENAI_EMBEDDING_MODEL
                )
                embedding = response.data[0].embedding
                
                # Create Qdrant point
                point_id = str(uuid.uuid4())
                points.append(PointStruct(
                    id=point_id,
                    vector=embedding,
                    payload={"content": content, **metadata}
                ))
            except Exception as e:
                print(f"Failed to process vector doc for Qdrant: {e}")
                
        if points:
            await self.qdrant_client.upsert(
                collection_name=self.collection_name,
                points=points
            )
