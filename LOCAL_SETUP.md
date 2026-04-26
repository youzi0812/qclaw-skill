# QClaw Skill 本地快速上手（用户版）

适用场景：从仓库导入后，希望在本机直接启用长期记忆能力，不依赖云服务器。

## 1) 下载代码

下载或克隆仓库到本地任意目录。

## 2) 一键启动（最简）

进入 `qclaw-skill` 目录，双击：

- `start_local.bat`（默认无鉴权）

或在 PowerShell 执行：

```powershell
cd qclaw-skill
.\start_local.ps1 -Mode noauth
```

启动成功后服务地址为：

- `http://127.0.0.1:8000`

如果 `8000` 被占用，脚本会自动尝试 `8001~8010`，并在终端打印实际端口。

## 3) 鉴权模式（可选）

```powershell
cd qclaw-skill
.\start_local.ps1 -Mode auth -ApiKey "your-api-key"
```

开启后调用方需带请求头：

```text
x-api-key: your-api-key
```

## 4) 验证是否可用

打开浏览器访问：

- `http://127.0.0.1:8000/v1/health`

如果返回 JSON 且 `ok=true`，说明服务可用。

## 5) 数据存储位置

长期记忆数据库默认在本地：

- `qclaw-skill/data/memory.db`

## 6) 停止服务

在启动服务的终端窗口按：

- `Ctrl + C`
