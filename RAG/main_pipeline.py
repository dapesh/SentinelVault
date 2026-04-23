# Role: The Orchestrator that ties the entire pipeline together.
# Justification: Provides a single asynchronous entry point to load documents, classify them, extract knowledge, and format the output.

import asyncio
import json
import os
from document_parser import UniversalParser
from domain_classifier import detect_document_context
from dynamic_extractor import ZeroShotExtractor
from graph_formatter import GraphFormatter

async def process_document(file_path: str):
    print(f"Starting pipeline for: {file_path}")
    
    # 1. Parsing
    print("[1/4] Parsing document...")
    parser = UniversalParser()
    try:
        parsed_doc = parser.parse(file_path)
    except FileNotFoundError as e:
        print(f"Error: {e}")
        return

    full_text = parsed_doc["full_text"]
    if not full_text:
        print("Error: Extracted text is empty.")
        return

    # 2. Classification
    print("[2/4] Classifying domain and inferring schema...")
    # Running blocking I/O (requests) in executor
    loop = asyncio.get_event_loop()
    context = await loop.run_in_executor(None, detect_document_context, full_text)
    print(f"Detected Context: {json.dumps(context, indent=2)}")

    suggested_schema = context.get("suggested_schema", [])

    # 3. Extraction
    print("[3/4] Extracting knowledge triples dynamically...")
    extractor = ZeroShotExtractor()
    
    # Running extraction in executor (since it's blocking requests)
    triples = await loop.run_in_executor(
        None, 
        extractor.sliding_window_extract, 
        full_text, 
        suggested_schema
    )
    print(f"Extracted {len(triples)} triples.")

    # 4. Graph Formatting
    print("[4/4] Formatting output for databases...")
    formatter = GraphFormatter()
    
    cypher_queries = formatter.to_neo4j_cypher(triples)
    vector_json = formatter.to_vector_db_json(triples, metadata=context)

    # 5. Output Manifest
    final_manifest = {
        "file_info": parsed_doc["metadata"],
        "classification": context,
        "triples": triples,
        "database_exports": {
            "neo4j_cypher": cypher_queries,
            "vector_db_json": vector_json
        }
    }

    output_file = "final_manifest.json"
    with open(output_file, "w", encoding="utf-8") as f:
        json.dump(final_manifest, f, indent=4)
        
    print(f"Pipeline completed successfully. Manifest saved to {output_file}")

if __name__ == "__main__":
    import sys
    
    if len(sys.argv) > 1:
        target_file = sys.argv[1]
    else:
        # Create a mock file for testing if no argument is provided
        target_file = "sample_document.txt"
        if not os.path.exists(target_file):
            print(f"Creating mock document: {target_file}")
            with open(target_file, "w") as f:
                f.write("Nuclear Safety Manual.\n\n")
                f.write("The primary coolant system is responsible for regulating the core temperature. ")
                f.write("Operators must ensure the valves remain open during Phase 1 operations. ")
                f.write("Failure to do so will trigger an immediate shutdown sequence.")
                
    asyncio.run(process_document(target_file))
