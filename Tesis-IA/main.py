from contextlib import asynccontextmanager
from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware

from src.core.config import settings
from src.db.database import init_db
from src.services.queue_manager import QueueManager
from src.api.routes import router as auditorias_router

@asynccontextmanager
async def lifespan(app: FastAPI):
    # Inicializar Base de Datos SQLite
    await init_db()
    # Iniciar Worker de cola en segundo plano
    await QueueManager.start_worker()
    yield

app = FastAPI(
    title=settings.PROJECT_NAME,
    version=settings.VERSION,
    lifespan=lifespan
)

# Habilitar CORS para comunicarse con Angular (http://localhost:4200) y .NET (http://localhost:5000 o similar)
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

app.include_router(auditorias_router, prefix=settings.API_V1_PREFIX)

@app.get("/health")
async def health_check():
    return {
        "status": "healthy",
        "service": settings.PROJECT_NAME,
        "version": settings.VERSION,
        "ollama_model": settings.OLLAMA_MODEL
    }

if __name__ == "__main__":
    import uvicorn
    uvicorn.run("main:app", host=settings.HOST, port=settings.PORT, reload=True)
