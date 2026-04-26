# SoulCore QClaw Skill

本项目为 QClaw 提供本地长期记忆与上下文增强能力：通过 FastAPI 后端实现 `chat`、`memory`、`diagnostics` 接口，支持跨轮偏好记忆与人格一致回复。

[![Install with npx skills](https://img.shields.io/badge/Install-npx%20skills-blue)](https://skills.sh/)

## Install

```bash
npx skills add youzi0812/qclaw-skill -g -y
```

## Quick Start (Local)

1. 下载仓库后进入 `qclaw-skill` 目录
2. 双击 `start_local.bat`（或 PowerShell 运行 `.\start_local.ps1 -Mode noauth`）
3. 访问 `http://127.0.0.1:8000/v1/health` 验证服务

> 若 `8000` 端口被占用，脚本会自动切换到 `8001~8010` 并打印实际端口。

## Repository Structure

- `SKILL.md`：平台导入入口
- `qclaw-thin-skill/`：薄 Skill 路由层与接口参考
- `qclaw-skill/`：后端服务实现与本地脚本
- `LOCAL_SETUP.md` / `使用说明.md`：用户使用说明

## Keywords

QClaw, long-term memory, context recall, persona consistency, local skill, FastAPI, memory assistant
