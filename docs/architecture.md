# Architecture

AIRE starts as a local-first .NET CLI with four project layers.

## CLI

Parses user commands and delegates workflow execution to orchestration.

## Core

Contains domain models and contracts for runs, agents, artifacts, steps, and tools.

## Orchestration

Coordinates run creation, state changes, pipeline execution, and failure handling.

## Infrastructure

Owns file-system operations, JSON serialization, logging, and tool executor implementations.

## Fake Agents

Milestone 1 uses fake agents to prove the workflow without real AI calls.

## Run Folder Contract

Each run is written under `runs/{run-id}` with `input`, `workspace`, `artifacts`, `reports`, `logs`, and `run-state.json`.
