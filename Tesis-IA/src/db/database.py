import aiosqlite
import json
from datetime import datetime
from typing import Optional, Dict, Any
from src.core.config import settings

async def init_db():
    """Inicializa las tablas de SQLite si no existen."""
    async with aiosqlite.connect(settings.DB_PATH) as db:
        # Tabla para las auditorías y estado en cola
        await db.execute("""
            CREATE TABLE IF NOT EXISTS auditorias (
                ticket_id TEXT PRIMARY KEY,
                tesis_id TEXT NOT NULL,
                titulo TEXT,
                alumno_id TEXT,
                archivo_path TEXT NOT NULL,
                estado TEXT NOT NULL,
                progreso_porcentaje INTEGER DEFAULT 0,
                mensaje TEXT,
                reporte_json TEXT,
                error TEXT,
                webhook_url TEXT,
                creado_en TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                actualizado_en TIMESTAMP DEFAULT CURRENT_TIMESTAMP
            )
        """)
        
        # Tabla de caché de citas para $0 costo y respuesta inmediata
        await db.execute("""
            CREATE TABLE IF NOT EXISTS cache_citas (
                clave_hash TEXT PRIMARY KEY,
                cita_texto TEXT NOT NULL,
                resultado_json TEXT NOT NULL,
                verificada_en TIMESTAMP DEFAULT CURRENT_TIMESTAMP
            )
        """)
        await db.commit()

def get_db_connection():
    return aiosqlite.connect(settings.DB_PATH)
