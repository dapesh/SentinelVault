class GraphFormatter:
    def __init__(self):
        pass

    def to_neo4j_cypher(self, triples: list[dict]) -> list[str]:
        """
        Converts a list of triples into Neo4j Cypher MERGE queries.
        Relies on the 'slug' strategy implemented in dynamic_extractor.py.
        """
        cypher_queries = []
        for t in triples:
            # We assume subject, predicate, and object are already cleanly slugified by the LLM
            subject_slug = t.get('subject', '').strip().replace('"', "'")
            predicate_slug = t.get('predicate', '').strip().replace(' ', '_').upper()
            object_slug = t.get('object', '').strip().replace('"', "'")
            context = t.get('context_source', '').replace('"', "'")
            
            if not subject_slug or not predicate_slug or not object_slug:
                continue
                
            # MERGE ensures we don't duplicate nodes, leveraging the unique slugs
            query = f"""
            MERGE (s:Entity {{id: "{subject_slug}"}})
            ON CREATE SET s.name = "{subject_slug}"
            MERGE (o:Entity {{id: "{object_slug}"}})
            ON CREATE SET o.name = "{object_slug}"
            MERGE (s)-[r:{predicate_slug} {{context: "{context}"}}]->(o)
            """
            cypher_queries.append(query.strip())
            
        return cypher_queries

    def to_vector_db_json(self, triples: list[dict], metadata: dict) -> list[dict]:
        """
        Converts extracted data into a JSON format suitable for Vector DBs (Qdrant), embedding metadata.
        """
        vector_docs = []
        for idx, t in enumerate(triples):
            doc = {
                # Qdrant typically expects an ID (often UUID, but here we can just pass the payload)
                "content": f"{t.get('subject')} {t.get('predicate')} {t.get('object')}",
                "metadata": {
                    "subject": t.get('subject'),
                    "predicate": t.get('predicate'),
                    "object": t.get('object'),
                    "context_source": t.get('context_source'),
                    **metadata
                }
            }
            vector_docs.append(doc)
            
        return vector_docs
