
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

**Goal**: Keep content (data) and presentation (HTML design) separate.

## Table of Contents

- [CVForge](#cvforge)
- [CVForge 🚀](#cvforge-)
  - [Table of Contents](#table-of-contents)
  - [How It Works](#how-it-works)
  - [Installation](#installation)
  - [Command Line Usage](#command-line-usage)
    - [Example:](#example)
  - [Example Data File (YAML)](#example-data-file-yaml)
    - [Optional: Tags](#optional-tags)
  - [Template Rules](#template-rules)
    - [1. `value-of="field"`](#1-value-offield)
    - [2. `repeat-for="field"`](#2-repeat-forfield)
    - [3. `if-exists="field"`](#3-if-existsfield)
    - [4. `value-of="field"` (List item itself)](#4-value-of-list-item-itself)
  - [Minimal Template Example](#minimal-template-example)
  - [Example Run](#example-run)
  - [Notes](#notes)
  - [License](#license)
  - [Author](#author)

## How It Works

```
YAML/JSON → Template Engine → HTML/PDF
```

- The data file contains your CV information
- The HTML template defines the layout and visual appearance
- CVForge merges them into the final output

## Installation

```bash
go build -o cvforge
```

## Command Line Usage

```
cvforge --template template.html --data resume.yaml --output resume.pdf --format pdf
```

| Parameter | Description | Required | Default |
|-----------|-------------|----------|---------|
| `--template`, `-t` | Path to HTML template file | Yes | - |
| `--data`, `-d` | Path to YAML or JSON data file | Yes | - |
| `--output`, `-o` | Output file path | No | output.pdf |
| `--format`, `-f` | Output format: `pdf` or `html` | No | pdf |
| `--verbose`, `-v` | Enable verbose logs | No | false |

### Example:

```bash
cvforge -t resume.html -d data.yaml -o result.pdf -f pdf
```

## Example Data File (YAML)

```yaml
name: "Kaan Alkan"
title: "Software Developer"

summary: "I build maintainable software systems."

experience:
  - title: "Software Developer"
    company: "Evosoft"
    start: "2022"
    end: "Present"
    description: "Full-stack development."
    responsibilities:
      - "Develop mobile apps using Flutter"
      - "Maintain backend services written in Go"
```

### Optional: Tags

You may attach `_tags` to any item. They are stored but not currently used for filtering.

```yaml
experience:
  - title: "Backend Developer"
    company: "Some Company"
    _tags: ["Go", "Distributed Systems"]
```

## Template Rules

Templates use special HTML attributes for dynamic content.

### 1. `value-of="field"`
Replaces the element's text with the value from data.

```html
<h1 value-of="name"></h1>
<p value-of="summary"></p>
```

### 2. `repeat-for="field"`
Repeats the element for each item in a list.

```html
<section repeat-for="experience">
  <h3 value-of="title"></h3>
  <p value-of="company"></p>
</section>
```

### 3. `if-exists="field"`
Removes the element if the field does not exist.

```html
<div if-exists="summary">
  <p value-of="summary"></p>
</div>
```

### 4. `value-of="name"` (List item itself)

```html
<ul repeat-for="responsibilities">
  <li value-of="responsibilities"></li>
</ul>
```

## Minimal Template Example

```html
<!DOCTYPE html>
<html>
<head>
  <meta charset="UTF-8">
  <title>CV</title>
  <style>
    /* Your CSS here */
  </style>
</head>
<body>
  <h1 value-of="name"></h1>
  <h2 value-of="title"></h2>

  <div if-exists="summary">
    <p value-of="summary"></p>
  </div>

  <h3>Experience</h3>
  <article repeat-for="experience">
    <strong value-of="title"></strong> - <span value-of="company"></span><br>
    <em value-of="start"></em> → <em value-of="end"></em>
    <p value-of="description"></p>

    <ul repeat-for="responsibilities">
      <li value-of="responsibilities"></li>
    </ul>
  </article>
</body>
</html>
```

## Example Run

```bash
cvforge -t templates/classic.html -d data/kaan.yaml -o output/kaan.pdf -f pdf
```

**Outputs:** `output/kaan.pdf`

## Notes

- Visual design is 100% controlled in HTML/CSS
- The Go code does not include layout logic
- Data is normalized internally for consistency

## License

MIT License

## Author

Kaan Alkan - [ikuruzum@gmail.com](mailto:ikuruzum@gmail.com)
