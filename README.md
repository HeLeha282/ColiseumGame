# ColiseumGame

игра про колизей бомбилка с давидом

3D action tower defense на Unity: защищаем портал в центре Колизея от врагов, спускающихся с ярусов.

## Где что лежит
- **[CLAUDE.md](CLAUDE.md)** — концепция игры, технические правила, план. Claude Code читает его автоматически.
- **[DEVLOG.md](DEVLOG.md)** — журнал разработки: каждый сделанный шаг и каждое принятое решение
  (что выбрали и почему). **Читать перед началом работы, дописывать после каждого шага** —
  чтобы мы вдвоём не делали одну работу дважды.

## Как начать работать
1. Установить: Git + Git LFS, Unity Hub + **Unity 2022.3.10f1** (строго эта версия), Python 3.10+,
   `uv` (`winget install astral-sh.uv`), Claude Code.
2. `git lfs install`, затем `git clone https://github.com/HeLeha282/ColiseumGame.git`.
3. Unity Hub → Add → Add project from disk → папка репозитория.
4. Claude Code в папке проекта подхватит MCP-сервер `unityMCP` из `.mcp.json` (связь с редактором Unity).

## Порядок работы
`git pull` → прочитать DEVLOG.md → сделать задачу → дописать запись в DEVLOG.md → commit → push.
