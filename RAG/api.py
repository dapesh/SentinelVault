import os
from fastapi import FastAPI, HTTPException, Request, UploadFile, File
from fastapi.responses import JSONResponse
from pydantic import BaseModel, ValidationError

# Pipeline Imports
from document_parser import UniversalParser, DoclingParserError
from domain_classifier import detect_document_context
from dynamic_extractor import ZeroShotExtractor
from graph_formatter import GraphFormatter
from retrieval_service import HybridRetrievalService, RRFMergeError
from database_service import DatabaseService

app = FastAPI(title="SecureVault API", description="Zero-Shot Extraction & Hybrid Retrieval Engine")

# --- Global Exception Handlers for Strict Fail-Fast ---

@app.exception_handler(DoclingParserError)
async def docling_parser_error_handler(request: Request, exc: DoclingParserError):
    return JSONResponse(
        status_code=400,
        content={"error": "DoclingParserError", "detail": str(exc)}
    )

@app.exception_handler(ValidationError)
async def pydantic_validation_error_handler(request: Request, exc: ValidationError):
    return JSONResponse(
        status_code=422,
        content={"error": "ValidationError", "detail": exc.errors()}
    )

@app.exception_handler(RRFMergeError)
async def rrf_merge_error_handler(request: Request, exc: RRFMergeError):
    return JSONResponse(
        status_code=422,
        content={"error": "RRFMergeError", "detail": str(exc)}
    )

@app.exception_handler(ValueError)
async def value_error_handler(request: Request, exc: ValueError):
    return JSONResponse(
        status_code=400,
        content={"error": "ValueError", "detail": str(exc)}
    )

@app.exception_handler(Exception)
async def generic_exception_handler(request: Request, exc: Exception):
    import traceback
    return JSONResponse(
        status_code=500,
        content={
            "error": "InternalServerError", 
            "detail": str(exc),
            "traceback": traceback.format_exc()
        }
    )

# --- Endpoints ---

class QueryRequest(BaseModel):
    query: str

@app.post("/ingest")
async def ingest_document(file: UploadFile = File(...)):
    """
    Ingests a document, extracts zero-shot knowledge triples, and formats for databases.
    """
    temp_file_path = f"temp_{file.filename}"
    try:
        # Save uploaded file temporarily for docling
        with open(temp_file_path, "wb") as buffer:
            buffer.write(await file.read())
            
        # 1. Parsing (Strict fail-fast)
        parser = UniversalParser()
        parsed_doc = parser.parse(temp_file_path)
        full_text = parsed_doc["full_text"]

        # 2. Classification
        context = await detect_document_context(full_text)
        suggested_schema = context.get("suggested_schema", [])

        # 3. Extraction (Async Sliding Window + Slug Strategy)
        extractor = ZeroShotExtractor()
        triples = await extractor.sliding_window_extract(full_text, suggested_schema)

        # 4. Graph Formatting
        formatter = GraphFormatter()
        cypher_queries = formatter.to_neo4j_cypher(triples)
        vector_docs = formatter.to_vector_db_json(triples, metadata=context)

        # 5. Push to Real Databases
        db_service = DatabaseService()
        try:
            await db_service.initialize()
            await db_service.push_to_neo4j(cypher_queries)
            await db_service.push_to_qdrant(vector_docs)
        finally:
            await db_service.close()

        return {
            "status": "success",
            "metadata": parsed_doc["metadata"],
            "classification": context,
            "extracted_triples_count": len(triples),
            "databases_ready": {
                "neo4j_queries_generated": len(cypher_queries),
                "qdrant_docs_generated": len(vector_docs)
            }
        }
    finally:
        if os.path.exists(temp_file_path):
            os.remove(temp_file_path)

@app.post("/query")
async def query_engine(req: QueryRequest):
    """
    Executes a hybrid retrieval query using RRF.
    """
    service = HybridRetrievalService()
    results = await service.query(req.query)
    
    return {
        "status": "success",
        "results": results
    }

if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="0.0.0.0", port=8000)
