import os
from docling.document_converter import DocumentConverter

class DoclingParserError(Exception):
    """Exception raised when Docling fails to parse a document."""
    pass

class UniversalParser:
    """
    UniversalParser handles the extraction of text from raw documents (PDF, etc.) into Markdown.
    It strictly uses docling and fails fast if parsing is unsupported or fails.
    """
    def __init__(self):
        # We assume docling is installed and handles internal model fetching.
        self.converter = DocumentConverter()
        
    def parse(self, file_path: str) -> dict:
        if not os.path.exists(file_path):
            raise FileNotFoundError(f"File not found: {file_path}")
            
        try:
            # Strictly use docling. No fallback to pymupdf or plain text.
            result = self.converter.convert(file_path)
            
            markdown_content = result.document.export_to_markdown()
            
            if not markdown_content.strip():
                raise DoclingParserError("Parsed document is empty.")
                
            return {
                "metadata": {"file_name": os.path.basename(file_path), "engine": "docling"},
                "full_text": markdown_content
            }
        except Exception as e:
            # We wrap the exception to provide a clear error type for our global exception handler
            raise DoclingParserError(f"Failed to parse document with docling: {str(e)}") from e
