# Unity Space Swarm | Final Project (High Performance Programming)


**Unity version:** 6.2 <br>
**Primary tech:** Unity Entities 1.4.2, C# Job System, Burst Compiler


## Overview

This is the final project for the *High Performance Programming* lecture at IT University of Copenhagen.
A simulation of a space battle: numerous small "fighter" entities use swarm behaviour to move toward and attack large capital ships. The project is implemented with a data-oriented design (DOTS/ECS) and focuses on profiling, scalability and performance optimizations.


### Goals
- Simulate thousands of small ships with efficient parallel systems
- Use DOTS (Entities + Jobs + Burst) and minimize managed-object overhead
- Profile and document performance bottlenecks and optimizations
- Implement clear DOD-style architecture and show refactorings for performance
