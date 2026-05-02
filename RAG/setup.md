# 🛠️ SecureVault Setup Guide

This guide provides step-by-step instructions for setting up the **SecureVault** environment from scratch on a brand-new desktop.

---

## 📋 Step 0: Prerequisites & Tooling

Before you begin, ensure your machine has the necessary software installed.

1.  **Git**: [Install Git](https://git-scm.com/downloads) to clone the codebase.
2.  **Python 3.10+**: [Download Python](https://www.python.org/downloads/). Ensure you check "Add Python to PATH" during installation.
3.  **Docker Desktop**: [Download Docker](https://www.docker.com/products/docker-desktop/).
    *   **IMPORTANT:** Ensure Docker is running and "Use the WSL 2 based engine" is enabled (if on Windows).
    *   Verify by running `docker --version` in your terminal.

---

## 📂 Step 1: Clone the Repository

Open your terminal (PowerShell, CMD, or Terminal) and run:

```bash
git clone https://github.com/your-username/SecureVault.git
cd SecureVault
```

---

## 🚀 Step 2: Infrastructure Setup (Databases)

SecureVault requires two specialized databases: **Neo4j** (Graph) and **Qdrant** (Vector). We use Docker to spin these up without needing manual installation.

1.  **Pull and Start Containers:**
    ```bash
    docker-compose up -d
    ```
    *This will download the official Neo4j and Qdrant images (~500MB) and start them in the background.*

2.  **Verify Database Availability:**
    *   **Neo4j UI:** [http://localhost:7474](http://localhost:7474)
        *   Default Credentials: `neo4j` / `securevault_password`
    *   **Qdrant Dashboard:** [http://localhost:6333/dashboard](http://localhost:6333/dashboard)

---

## 🔑 Step 3: Environment Configuration

The application needs API keys to communicate with LLMs (Fireworks.ai) and Embedding models (OpenAI).

1.  **Create the `.env` file:**
    In the root directory, create a new file named `.env`.
2.  **Copy and fill the following template:**

```env
# LLM Extraction (Fireworks.ai)
FIREWORKS_API_KEY=your_fireworks_api_key

# Embeddings (OpenAI)
OPENAI_API_KEY=your_openai_api_key

# Neo4j (Graph)
NEO4J_URI=bolt://localhost:7687
NEO4J_USER=neo4j
NEO4J_PASSWORD=securevault_password

# Qdrant (Vector)
QDRANT_URL=http://localhost:6333
```

---

## 🐍 Step 4: Python Environment Setup

We recommend using a Virtual Environment to keep dependencies isolated.

1.  **Initialize Virtual Env:**
    ```bash
    python -m venv .venv
    ```
2.  **Activate:**
    *   **Windows:** `.venv\Scripts\activate`
    *   **Mac/Linux:** `source .venv/bin/activate`
3.  **Install Requirements:**
    ```bash
    pip install --upgrade pip
    pip install -r requirements.txt
    ```

---

## ⚡ Step 5: Start SecureVault

Once everything is configured, start the FastAPI backend:

```bash
uvicorn api:app --host 0.0.0.0 --port 8000 --reload
```

*   **API Docs:** [http://localhost:8000/docs](http://localhost:8000/docs)

---

## ✅ Step 6: Verification Test

Run the automated test suite to confirm that the ingestion pipeline and retrieval engine are communicating with the databases correctly:

```bash
python test_architecture.py
```

### Troubleshooting
*   **"Docker command not found":** Ensure Docker Desktop is installed and you've restarted your terminal.
*   **"Port 7687 already in use":** You might have another instance of Neo4j running. Stop it before running `docker-compose`.
*   **"ModuleNotFoundError":** Ensure you have activated your virtual environment (Step 4).
