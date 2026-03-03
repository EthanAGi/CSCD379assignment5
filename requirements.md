# StorySmith (Working Title) — Requirements

## Elevator Pitch (1–4 sentences)
StorySmith is an AI-enabled writing companion that automatically builds a “Story Bible” from your manuscript: characters, locations, themes, and arcs. It helps writers stay consistent, find relevant context instantly using semantic (vector) search, and receive targeted revision feedback. A stretch feature, image generation turns a passage into concept art for characters or scenes to support visualization and inspiration, possibly to create cover art or promotional material. Another stretch goal would be having AI read your passages, creating audio books or simply saying a passage back to hear it out loud.
- Screenwriters / tabletop campaign writers
- Students writing long-form creative work
- Writing groups collaborating on shared universes

## User Goals
- Keep track of character details and prevent contradictions
- Search the manuscript by meaning, not keywords
- Get feedback on clarity, pacing, tone, and theme consistency
- Plan story arcs and ensure scenes support them
- Visualize characters/scenes (optional image generation)

---

## Core Features (MVP)
### 1) Projects and Manuscripts
- Create a writing Project
- Add Chapters/Scenes (text editor)
- Versioning (basic): save revisions with timestamps

### 2) AI Story Bible Extraction
From chapters/scenes, AI extracts and updates:
- Characters (name, aliases, description, traits, relationships)
- Locations (name, description, key details)
- Themes (e.g., “found family”, “revenge”, “loss”)
- Story arcs (main arc + character arcs if possible)
- Important facts (e.g., “injury on left arm”, “blue eyes”, “born in 2004”)

### 3) Consistency & Continuity Checks
- Flag contradictions (e.g., eye color changes, timeline conflicts)
- Warn when a scene conflicts with established facts
- Show citations to the exact passages that support each fact

### 4) Semantic (Vector) Search
- Ask: “Where did I mention X?” or “Find scenes like this tone”
- Search across chapter text + notes using embeddings
- Return top results with snippet previews and links

### 5) AI Writing Review
- User selects a passage and chooses a review mode:
  - Clarity + readability
  - Style/tone match (e.g., “make it more noir”)
  - Pacing suggestions
  - Theme reinforcement
- Output: actionable bullet suggestions + optional rewritten example

---

## Stretch Features (Nice-to-have)
- Image generation: character portrait / scene concept art from selected passage
- **AI Audiobook / Voice Narration:** generate an audio reading of a selected passage/chapter using an AI voice (choose voice style/speed). Store and replay audio per chapter.
- Collaboration: multiple users per project with comments
- Timeline tool: events and ordering
- Export: Story Bible PDF / JSON

---

## Primary Use Cases
1. **New project setup**
   - User creates a project, pastes chapter 1, runs “Extract Story Bible”
2. **Continuity check before publishing a chapter**
   - User runs “Check Consistency” → app shows flagged issues with citations
3. **Find context quickly**
   - User searches “when did she get the scar?” → returns relevant snippets
4. **Revise a scene**
   - User selects a passage → “Pacing review” → suggestions + optional rewrite
5. **Visualize a scene (stretch)**
   - User selects a passage → “Generate scene concept art” → stores image + prompt
6. **Listen to writing out loud (stretch)**
   - User selects a chapter/passage → chooses a voice → “Generate narration” → app stores audio and plays it back (helpful for catching awkward phrasing)

---

## Tech Stack
### Frontend
- Nuxt 3 (Vue 3)
- Vuetify for UI components
- TypeScript
- Vitest for unit tests

### Backend
- ASP.NET Core Web API (.NET 8)
- Controllers + Services + DTOs
- Entity Framework Core

### Database
- Azure SQL Database
- Tables: Users, Projects, Chapters, Entities (Characters/Locations/Themes/Arcs), Facts, Notes, Embeddings, Images, AudioNarrations

### AI + Vector Search
- AI endpoints:
  - Text analysis/extraction (LLM)
  - Embeddings generation
  - Optional image generation
  - Optional text-to-speech (AI voice) narration
- Vector search approach:
  - Store embeddings in DB and compute similarity in API
  - OR use a managed vector DB/service (if approved/available)
- Requirement: semantic search must be part of core functionality

### Hosting / Deployment
- Azure App Service (API)
- Azure Static Web Apps OR Azure App Service (frontend)
- GitHub Actions CI/CD pipeline for both

---

## Technical Requirements (What must be true)
- Auth: login/register (JWT or cookie-based)
- Role support (optional): Admin/User
- Clean architecture:
  - Controllers call services
  - Services contain business logic
  - DTOs for request/response
  - EF Core repositories (optional) or DbContext in services
- Vector search implemented and used in at least one core feature
- AI used as core functionality (extraction + review)
- Unit tests:
  - Backend: service-level tests for extraction pipeline + search ranking logic
  - Frontend: tests for key composables/components (e.g., search UI logic, editor actions)
- CI/CD:
  - Build + test on PR/push
  - Deploy on push to main

---

## Data Model (Draft)
- Project(id, ownerId, title, description, createdAt)
- Chapter(id, projectId, title, content, createdAt, updatedAt)
- Entity(id, projectId, type, name, summaryJson, updatedAt)
- Fact(id, projectId, entityId?, factType, value, sourceChapterId, sourceQuote, confidence)
- Embedding(id, projectId, chapterId?, noteId?, vector, text, createdAt)
- Image(id, projectId, chapterId?, prompt, imageUrl, createdAt)
- AudioNarration(id, projectId, chapterId?, passageHash?, voice, speed, audioUrl, createdAt)

---

## Non-Functional Requirements
- Responsive UI (desktop + mobile)
- Great UX:
  - Clean dashboard
  - Fast search
  - Clear citations for extracted facts
- Performance:
  - Vector search returns in < 1s for typical projects
- Security:
  - Users can only access their own projects

---

## Definition of Done
- Deployed app is usable end-to-end:
  - Create project → add chapters → extract story bible → search → review → consistency check
- Tests pass in CI
- Presentation slides ready and demo script rehearsed
