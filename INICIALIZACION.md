# Inicialización SDD (blackhellheim-api)

Mismo procedimiento en `blackhellheim-web`. Este zip es una **semilla**: se descomprime DESPUÉS de `specify init`, porque `specify init` crea su propia `constitution.md` plantilla y podría sobrescribir la nuestra.

## Requisitos
Python 3.11+, git, uv, y el agente (Claude Code y/o OpenCode).

## Pasos
```bash
# 1. Instalar la CLI (una vez)
uv tool install specify-cli --from git+https://github.com/github/spec-kit.git
specify check

# 2. Crear el repositorio vacío en GitHub (público) y clonarlo
git clone https://github.com/<usuario>/blackhellheim-api.git
cd blackhellheim-api

# 3. Inicializar Spec Kit en la carpeta actual
specify init . --ai claude          # añade --force si la carpeta ya tiene archivos
#    Windows: agrega --script ps

# 4. (opcional) soporte para OpenCode
specify init . --ai opencode --force   # en versiones nuevas: specify integration list

# 5. Descomprimir esta semilla encima (acepta sobrescribir constitution.md)
unzip -o ../blackhellheim-api-sdd-semilla.zip -d .

# 6. Verificar en el agente y confirmar la constitución
/speckit.constitution   # revisa que respete .specify/memory/constitution.md
git add . && git commit -m "Inicializa SDD" && git push
```

## Qué crea `specify init` y qué aporta la semilla
- `specify init`: `.specify/` (plantillas y scripts), carpeta de comandos del agente (`.claude/commands/`) y, al usar `/speckit.specify`, la carpeta `specs/NNN-.../`.
- Semilla: constitución en español, `AGENTS.md`, `CLAUDE.md`, specs 000, 900 y 901, `docs/`.

## Notas
- Los nombres de plantillas y scripts de Spec Kit quedan en inglés; no se renombran para no romper la herramienta. Lo que escribe tu equipo va en español.
- `specify init <nombre>` falla si la carpeta ya existe; por eso se usa `.`.
- Numeración: Spec Kit numera las specs por su cuenta. Para usar el número de historia, indícalo al crear la spec; si tu versión no lo permite, renombra la carpeta y la rama después de crearla (a confirmar con tu versión instalada).
- Con OpenCode y Claude Code a la vez, `AGENTS.md` es la fuente común y `CLAUDE.md` solo la importa.
