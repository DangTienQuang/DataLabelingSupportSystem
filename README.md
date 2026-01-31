# Data Labeling Support System

## Overview
This is the Backend API for the Data Labeling Support System. The system is designed to manage the workflow of data annotation projects, connecting Admins, Annotators, and Reviewers.

It handles project management, task assignment, image labeling processes, and quality assurance.

## Tech Stack
* **Framework:** .NET 8 Web API (Core)
* **Database:** SQL Server
* **ORM:** Entity Framework Core (Code First)
* **Authentication:** JWT (JSON Web Tokens)
* **Architecture:** N-Layer Architecture
    * `API`: Controllers & Entry point
    * `BLL`: Business Logic Layer
    * `DAL`: Data Access Layer (Repositories)
    * `Core`: Entities, DTOs, Enums, Interfaces

## Key Features
* **User Management:**
    * Register/Login with JWT.
    * Role-based authorization (Admin, Manager, Annotator, Reviewer).
    * **Soft Delete:** Users are deactivated (`IsActive = false`) instead of being removed from DB.
    * **Ban/Unban:** Admin can toggle user status via API.
* **Project Workflow:**
    * Create projects, define labels/tags.
    * Upload datasets.
* **Task Lifecycle:**
    * `New` -> `Assigned` -> `InProgress` -> `Submitted` -> `Approved` / `Rejected`.
* **Statistics:** Track user performance and project progress.

## Project Structure
```text
DataLabelingSupportSystem/
├── API/              # Controllers, Configurations, Swagger
├── BLL/              # Services, Business Logic
├── DAL/              # DbContext, Repositories, Migrations
└── Core/             # Entities, Request/Response Models, Enums
