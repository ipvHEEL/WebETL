from fastapi import FastAPI
from pydantic import BaseModel
from typing import Optional
import subprocess
import asyncio

app = FastAPI()

# Модель для тела запроса (если нужно передавать параметры)
class RunScriptRequest(BaseModel):
    script_path: Optional[str] = "E:\\scripts\\sync_reports_from_ssrs.ps1"  # путь по умолчанию

@app.post("/run-powershell")
async def run_powershell_script(request: RunScriptRequest):
    script_path = request.script_path

    try:
        cmd = [
            "powershell.exe",
            "-ExecutionPolicy", "Bypass",
            "-File", script_path
        ]

        # Запускаем процесс асинхронно
        process = await asyncio.create_subprocess_exec(
            *cmd,
            stdout=asyncio.subprocess.PIPE,
            stderr=asyncio.subprocess.PIPE
        )

        stdout, stderr = await asyncio.wait_for(process.communicate(), timeout=90)

        output = stdout.decode('utf-8')
        error = stderr.decode('utf-8')

        if process.returncode == 0:
            return {"status": "success", "message": "PowerShell-скрипт выполнен успешно", "output": output}
        else:
            return {"status": "error", "message": "Ошибка выполнения скрипта", "error": error}

    except asyncio.TimeoutError:
        return {"status": "timeout", "message": "Скрипт выполнялся дольше 90 секунд и был остановлен"}
    except Exception as e:
        return {"status": "exception", "message": str(e)}