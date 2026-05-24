# Sample Markdown Document

This file demonstrates all the rendering features of [mdr](https://github.com/michaelsanford/mdr).

## Text Formatting

This is a paragraph with **bold text**, *italic text*, and ***bold italic*** together. You can also use `inline code` within a sentence.

Here's another paragraph to show spacing. It contains a [hyperlink](https://github.com/michaelsanford/mdr) and some more `inline code snippets` for good measure.

## Lists

### Unordered

- First item
- Second item with **bold**
- Third item with nested children
  - Nested item A
  - Nested item B
    - Deeply nested
- Fourth item

### Ordered

1. Install the .NET 9 SDK
2. Clone the repository
3. Build the project
4. Run `mdr SAMPLE.md`

## Blockquotes

> This is a blockquote. It should render with a vertical bar and italic styling.

> "Any sufficiently advanced technology is indistinguishable from magic."
> — Arthur C. Clarke

## Code Blocks

### C#

```csharp
using System;

public class Program
{
    public static async Task Main(string[] args)
    {
        var message = "Hello, World!";
        Console.WriteLine(message);

        // Async example
        await Task.Delay(1000);
        var numbers = new List<int> { 1, 2, 3, 4, 5 };
        foreach (var n in numbers)
        {
            if (n % 2 == 0)
                Console.WriteLine($"{n} is even");
        }
    }
}
```

### JavaScript

```javascript
import express from 'express';

const app = express();

async function fetchData(url) {
    const response = await fetch(url);
    if (!response.ok) {
        throw new Error(`HTTP ${response.status}`);
    }
    return response.json();
}

// Arrow function with destructuring
const processItems = (items) => {
    return items.filter(item => item.active)
                .map(({ name, value }) => ({ name, total: value * 2 }));
};

export default app;
```

### TypeScript

```typescript
interface User {
    id: number;
    name: string;
    email: string;
}

type Result<T> = { success: true; data: T } | { success: false; error: string };

async function getUser(id: number): Promise<Result<User>> {
    const response = await fetch(`/api/users/${id}`);
    if (!response.ok) {
        return { success: false, error: "Not found" };
    }
    const data = await response.json();
    return { success: true, data };
}
```

### Python

```python
from dataclasses import dataclass
from typing import Optional

@dataclass
class Config:
    host: str
    port: int = 8080
    debug: bool = False

async def fetch_records(db, query: str) -> list[dict]:
    """Fetch records from the database."""
    results = []
    async with db.connection() as conn:
        for row in await conn.execute(query):
            if row is not None:
                results.append(dict(row))
    return results

# Lambda and comprehension
transform = lambda x: x ** 2
squares = [transform(n) for n in range(10) if n % 2 == 0]
```

### Rust

```rust
use std::collections::HashMap;

pub struct Cache<T> {
    store: HashMap<String, T>,
}

impl<T: Clone> Cache<T> {
    pub fn new() -> Self {
        Self { store: HashMap::new() }
    }

    pub fn get(&self, key: &str) -> Option<&T> {
        self.store.get(key)
    }

    pub async fn fetch_or_insert(&mut self, key: &str, f: impl AsyncFn() -> T) -> &T {
        if !self.store.contains_key(key) {
            let value = f().await;
            self.store.insert(key.to_string(), value);
        }
        self.store.get(key).unwrap()
    }
}

fn main() {
    let mut cache = Cache::new();
    let result = cache.get("hello");
    match result {
        Some(val) => println!("Found: {}", val),
        None => println!("Not found"),
    }
}
```

### Go

```go
package main

import (
    "fmt"
    "sync"
)

type Worker struct {
    ID   int
    Jobs chan func()
}

func NewPool(size int) *sync.WaitGroup {
    var wg sync.WaitGroup
    for i := range size {
        wg.Add(1)
        go func(id int) {
            defer wg.Done()
            fmt.Printf("Worker %d started\n", id)
        }(i)
    }
    return &wg
}

func main() {
    pool := NewPool(5)
    pool.Wait()
    fmt.Println("All workers done")
}
```

## Tables

### Simple Table

| Feature | Status | Notes |
|---------|--------|-------|
| Headings | ✅ | Color-coded by level |
| Bold/Italic | ✅ | Native ANSI formatting |
| Code blocks | ✅ | 6 languages supported |
| Tables | ✅ | Full-width, rounded borders |
| Lists | ✅ | Ordered, unordered, nested |
| Blockquotes | ✅ | Vertical bar + italic |
| Links | ✅ | Underlined with URL |

### Data Table

| Language | Extension | Keywords | Year |
|----------|-----------|----------|------|
| C# | .cs | 34 | 2000 |
| JavaScript | .js | 24 | 1995 |
| TypeScript | .ts | 24 | 2012 |
| Python | .py | 27 | 1991 |
| Rust | .rs | 24 | 2010 |
| Go | .go | 21 | 2009 |

## Horizontal Rules

Content above the rule.

---

Content below the rule.

***

Another section after a different rule syntax.

## Links

- GitHub: [mdr repository](https://github.com/michaelsanford/mdr)
- Docs: [Markdig](https://github.com/xoofx/markdig)
- Docs: [Spectre.Console](https://spectreconsole.net/)
- Download: [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)

## Nested Formatting

- **Bold list item** with `code` inside
- *Italic item* with a [link](https://example.com)
- Regular item with ***bold italic*** emphasis
  1. Nested ordered with **formatting**
  2. Another nested item
