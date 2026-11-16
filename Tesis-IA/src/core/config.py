from pydantic_settings import BaseSettings
from pathlib import Path

class Settings(BaseSettings):
    PROJECT_NAME: str = "Tesis-IA Auditor Service"
    VERSION: str = "1.0.0"
    API_V1_PREFIX: str = "/api/v1"
    
    # Servidor
    HOST: str = "0.0.0.0"
    PORT: int = 8000
    
    # Ollama Local
    OLLAMA_BASE_URL: str = "http://localhost:11434"
    OLLAMA_MODEL: str = "qwen2.5-coder:7b"  # 5GB, 55% GPU / 45% CPU, respuesta en ~10s
    
    # APIs Académicas (Polite pool - 100% gratis)
    CROSSREF_MAILTO: str = "tesis.auditor@institucional.edu.mx"
    SEMANTIC_SCHOLAR_BASE_URL: str = "https://api.semanticscholar.org/graph/v1"
    
    # Base de Datos SQLite (Cola y Caché)
    DB_PATH: Path = Path("tesis_auditor.db")
    UPLOAD_DIR: Path = Path("temp_uploads")
    
    # Concurrencia
    MAX_CONCURRENT_LLM_JOBS: int = 1  # 1 a la vez para no saturar los 4GB de GPU
    MAX_CONCURRENT_CITATION_FETCHES: int = 5  # Consultas de red simultáneas

    class Config:
        env_file = ".env"
        extra = "allow"

settings = Settings()
settings.UPLOAD_DIR.mkdir(exist_ok=True, parents=True)
