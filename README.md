# CVForge 🚀

<p align="center">
  <img src="https://img.shields.io/badge/Go-1.21+-00ADD8?style=for-the-badge&logo=go" alt="Go Version" />
  <img src="https://img.shields.io/badge/License-MIT-green?style=for-the-badge" alt="License" />
  <img src="https://img.shields.io/badge/Status-Active-success?style=for-the-badge" alt="Status" />
</p>

<p align="center">
  Modern, powerful and flexible CV/Resume template engine for Go
</p>

---

## 📋 Table of Contents

- [Features](#-features)
- [Installation](#-installation)
- [Quick Start](#-quick-start)
- [Template Syntax](#-template-syntax)
- [Data Structure](#-data-structure)
- [CLI Usage](#-cli-usage)
- [Examples](#-examples)
- [Advanced Features](#-advanced-features)
- [Contributing](#-contributing)
- [License](#-license)

---

## ✨ Features

- 🎨 **Simple Template Syntax** - Easy-to-use HTML attributes for data binding
- 🔄 **Smart Iterations** - Repeat HTML elements with `repeat-for`
- 🎯 **Conditional Rendering** - Show/hide sections with `if-exist`
- 🔗 **Auto-linking** - Automatic URL detection and hyperlink creation
- 📄 **Multiple Outputs** - Generate PDF or HTML
- 🏷️ **Tag Filtering** - Filter CV sections by tags
- ⚡ **Fast & Lightweight** - Built with Go for maximum performance
- 🎭 **Headless Chrome** - High-quality PDF generation with chromedp
- 🛠️ **CLI Ready** - Easy command-line interface

---

## 📦 Installation

### Prerequisites

- Go 1.21 or higher
- Chrome/Chromium (for PDF generation)

### Install

```bash
# Clone the repository
git clone https://github.com/yourusername/cvforge.git
cd cvforge

# Download dependencies
go mod download

# Build
go build -o cvforge

# (Optional) Install globally
go install