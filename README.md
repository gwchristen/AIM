# AIM - Asset Inventory Management

<div align="center">

![Status](https://img.shields.io/badge/status-Active-brightgreen)
![Platform](https://img.shields.io/badge/platform-Windows-blue)
![Language](https://img.shields.io/badge/language-C%23-purple)
![WinUI 3](https://img.shields.io/badge/WinUI-3-blue)

A professional Windows desktop application for comprehensive asset inventory management with enterprise-grade security, audit logging, and customizable theming.

</div>

---

## 📋 Table of Contents

- [Overview](#overview)
- [Features](#features)
- [System Requirements](#system-requirements)
- [Installation](#installation)
- [Quick Start](#quick-start)
- [Architecture](#architecture)
- [Security Model](#security-model)
- [Configuration](#configuration)
- [Usage Guide](#usage-guide)
- [Development](#development)
- [Contributing](#contributing)
- [License](#license)

---

## Overview

**AIM (Asset Inventory Management)** is a robust Windows desktop application built with WinUI 3 and C#. It provides organizations with a comprehensive solution for managing, tracking, and auditing their asset inventory. With enterprise-grade security, detailed audit logging, and a modern user interface, AIM ensures complete transparency and control over your organization's assets.

### Key Highlights

- 🔒 **Enterprise Security** - Master password override, role-based access control, and authorized user management
- 📊 **Comprehensive Auditing** - Every action is logged with timestamp, user, and details
- 🎨 **Modern Theming** - Support for Light, Dark, High Contrast, and system-following themes
- 📁 **Directory Operations** - Browse, analyze, and manage directory structures
- 🔍 **Advanced Search** - Search by filename or file content across your inventory
- 📝 **Form Generation** - Create, customize, and print inventory forms
- 💾 **Encrypted Settings** - Security configuration stored securely with AES encryption

---

## Features

### Core Features

| Feature | Description |
|---------|-------------|
| **Asset Browsing** | Navigate and explore directory structures with an intuitive tree view |
| **File Management** | View, preview, and manage files within your inventory system |
| **Advanced Search** | Full-text search across filenames and file contents |
| **Inventory Management** | Organize, archive, and track inventory with detailed metadata |
| **Form Generation** | Generate printable forms with customizable templates (Ohio, I&M) |
| **Batch Operations** | Clone directories, rename files, and perform batch operations |
| **Directory Analysis** | Analyze directory structures and generate reports |

### Security Features

| Feature | Description |
|---------|-------------|
| **Master Password** | Override authentication for administrative access |
| **Authorized Users** | Whitelist specific Windows users for access control |
| **Encrypted Storage** | Security configuration encrypted with AES-256 |
| **Audit Logging** | Complete audit trail of all system activities |
| **Session Management** | Lock/unlock sensitive features on demand |

### User Experience Features

| Feature | Description |
|---------|-------------|
| **Light Theme** | Bright, clean interface for well-lit environments |
| **Dark Theme** | Eye-friendly interface for low-light conditions |
| **High Contrast Mode** | Enhanced visibility for accessibility needs |
| **System Theme Sync** | Automatically follows Windows theme settings |
| **Windows Accent Color** | Uses your system accent color throughout the app |

---

## System Requirements

### Minimum Requirements

- **OS**: Windows 10 (Build 19041) or Windows 11
- **Runtime**: .NET 8.0 or later
- **RAM**: 2 GB minimum (4 GB recommended)
- **Disk Space**: 500 MB for installation
- **Display**: 1366 x 768 resolution minimum

### Development Requirements

- **Visual Studio 2022** or later
- **.NET 8.0 SDK**
- **Windows App SDK (WinUI 3)** workload installed
- **C# 12** or later support

---

## Installation

### From Released Build

1. Download the latest release from the [Releases](https://github.com/gwchristen/AIM/releases) page
2. Run the installer and follow the on-screen prompts
3. Launch AIM from your Start Menu or Desktop shortcut

### From Source Code

1. **Clone the repository**:
   ```bash
   git clone https://github.com/gwchristen/AIM.git
   cd AIM
