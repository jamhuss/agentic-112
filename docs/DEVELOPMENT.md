# Development Guidelines

## Architecture

We are building a backend system with AI as a component.

Structure:

- Api → Controllers
- Application → Business logic
- Domain → Core models
- Infrastructure → AI + persistence

---

## Core Rule

All incident creation must result in the SAME entity.

There are two sources:

1. Manual (user-defined)
2. AI (model-generated)

---

## Incident Model

All flows produce:

- Description
- Services (must be from predefined list)
- Priority
- CreatedBy ("User" or "AI")

AI adds:
- Confidence
- Credibility
- NeedsHumanReview

---

## AI Integration

The system uses a single abstraction:

IAiGateway

Responsibilities:
- Build prompt
- Call LLM
- Return structured JSON

---

## AI Constraints

The AI MUST:

- Only use allowed services:
  - Ambulance
  - Police
  - Fire_Department
  - Assistance

- Never invent new services
- Always return structured JSON
- Never return free text

---

## Validation Rules

All AI responses must be validated:

- Services must exist in allowed list
- Priority must be known value
- If invalid → reject or fallback

---

## Development Strategy

1. Start with fake AI responses
2. Build full flow
3. Replace with real LLM

---

## Coding Rules

- No business logic in controllers
- AI calls only via IAiGateway
- No direct model calls in services
- Keep models simple and explicit

---

## Goal

The goal is NOT:
- to build the smartest AI

The goal IS:
- to build a correct system around AI