import os
from openai import AsyncOpenAI
from dotenv import load_dotenv

load_dotenv(override=True)

# Pre-configured model constants
FIREWORKS_CLASSIFIER_MODEL = "accounts/fireworks/models/qwen3-8b"
FIREWORKS_EXTRACTOR_MODEL = "accounts/fireworks/models/llama-v3p3-70b-instruct"
OPENAI_EMBEDDING_MODEL = "text-embedding-3-small"

def get_fireworks_client() -> AsyncOpenAI:
    api_key = os.getenv("FIREWORKS_API_KEY")
    if not api_key:
        raise ValueError("FIREWORKS_API_KEY environment variable is not set")
        
    return AsyncOpenAI(
        base_url="https://api.fireworks.ai/inference/v1",
        api_key=api_key
    )

def get_openai_client() -> AsyncOpenAI:
    api_key = os.getenv("OPENAI_API_KEY")
    if not api_key:
        raise ValueError("OPENAI_API_KEY environment variable is not set")
        
    return AsyncOpenAI(api_key=api_key)
