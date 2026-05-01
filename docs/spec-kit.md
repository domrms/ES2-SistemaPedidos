# Configuração de Ambiente com Spec Kit

Este documento descreve como preparar uma nova máquina para desenvolver e executar este projeto utilizando **Spec Kit**.

> **Importante:** O repositório já foi clonado. Este guia cobre apenas a configuração local da máquina.

---

# Visão Geral

Este projeto utiliza:

* **Python 3**
* **Ambiente virtual (venv)**
* **UV** para gerenciamento moderno de dependências
* **Spec Kit** para padronização de especificações e fluxo de desenvolvimento

---

# Pré-requisitos

Instale os softwares abaixo:

* **Python 3.x**
* **Git**
* **Visual Studio Code** (recomendado)

---

# 1. Validar Python instalado

No terminal, execute:

```bash
python --version
```

ou:

```bash
py --version
```

Se não funcionar no Windows, reinstale o Python marcando:

```text
Add Python to PATH
```

---

# 2. Criar ambiente virtual

Na raiz do projeto:

```bash
python -m venv venv
```

ou:

```bash
py -m venv venv
```

---

# 3. Ativar ambiente virtual

## Windows (Git Bash)

```bash
source venv/Scripts/activate
```

## Windows (CMD)

```cmd
venv\Scripts\activate.bat
```

## Windows (PowerShell)

```powershell
venv\Scripts\Activate.ps1
```

## Linux / macOS

```bash
source venv/bin/activate
```

Após ativar, o terminal exibirá:

```bash
(venv)
```

---

# 4. Instalar UV

O projeto utiliza **UV** para dependências e ferramentas.

```bash
pip install uv
```

Validar instalação:

```bash
uv --version
```

---

# 5. Instalar Spec Kit

Instale a CLI oficial do **Spec Kit**:

```bash
uv tool install specify-cli --from git+https://github.com/github/spec-kit.git
```

---

# 6. Validar instalação do Spec Kit

```bash
uv tool list
```

Resultado esperado:

```text
specify-cli
- specify
```

---

# 7. Instalar dependências do projeto

## Se existir requirements.txt

```bash
pip install -r requirements.txt
```

## Se utilizar pyproject.toml

```bash
uv sync
```

---

# 8. Validar comandos do Spec Kit

Testar:

```bash
uv tool run specify --help
```

Se configurado no PATH:

```bash
specify --help
```

---

# 9. Executar aplicação

Após ativar o ambiente:

```bash
python main.py
```

> Ajustar conforme o ponto de entrada real do projeto.

---

# Fluxo diário de uso

Sempre que abrir o projeto:

```bash
source venv/Scripts/activate
```

Depois:

```bash
uv sync
```

ou

```bash
pip install -r requirements.txt
```

E então execute normalmente.

---

# Problemas comuns

## Python não encontrado no Windows

Use:

```bash
py
```

ou reinstale com PATH habilitado.

---

## Ambiente virtual não ativa no Git Bash

Use:

```bash
source venv/Scripts/activate
```

---

## Comando specify não encontrado

Use:

```bash
uv tool run specify --help
```

---

## Dependências quebradas

Reinstale:

```bash
uv sync
```

ou:

```bash
pip install -r requirements.txt
```

---

# Resumo Rápido

```bash
python -m venv venv
source venv/Scripts/activate
pip install uv
uv tool install specify-cli --from git+https://github.com/github/spec-kit.git
uv sync
```

---

# Importante

Este projeto utiliza **Spec Kit** como parte do fluxo oficial de desenvolvimento.
Mantenha o ambiente atualizado e utilize os comandos padronizados da ferramenta sempre que necessário.
