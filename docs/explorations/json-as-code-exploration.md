I meant as a way to generate code from a json-based specification, which should live as data models in the database, for example:

feature design doc:
```json
{
  "id": "FEATURE-123",
  "name": "state-health-subtype-feature",
  "description": "Implementation of the s zoning health subtype",
  "initial-context": "the s zoning is currently implemented, but no subtype is implemented yet, no game assets like sprite prefabs, specific audio, or game logic is implemented yet",
  "purpose": "To implement the state health subtype feature in the game logic, UI, and other systems",
  "scope": "full implementation for game MVP release",
  "definitions": [
    {
      "id": "DEFINITION-123",
      "name": "city health coverage",
      "definition": "Health coverage is city-wide, not area-specific"
    } /* more definitions */
  ],
  "references": [
    {
      "id": "REFERENCE-123",
      "name": "game MVP release health scope",
      "document": "docs/game-mvp-release-health-scope.md",
      "section": "1.2"
    } /* more references */
  ],
  "user-requirements": {
    "functional-user-requirements": [
        {
          "id": "USER-REQUIREMENT-123",
          "name": "state-health-game-economy",
          "description": "The state health subtype should have economic model that is integrated with the game economy system",
          "criticality": "high",
          "priority": "high",
          "status": "pending"
        } /* more functional requirements */
      {
        "id": "USER-REQUIREMENT-124",
        "name": "state-health-game-ui",
        "description": "The state health subtype should have a UI that is integrated with the game UI system",
        "criticality": "medium",
        "priority": "medium",
        "status": "pending"
      },
      {
        "id": "USER-REQUIREMENT-125",
        "name": "state-health-game-audio",
        "description": "The state health subtype should have a game audio that is integrated with the game audio system",
        "criticality": "medium",
        "priority": "medium",
        "status": "pending"
      },
      {
        "id": "USER-REQUIREMENT-126",
        "name": "state-health-game-social-impact",
        "description": "The state health subtype should have a social impact model that is integrated with the game social impact system, like happiness and desirability",
        "criticality": "medium",
        "priority": "medium",
        "status": "pending"
      },
      {
        "id": "USER-REQUIREMENT-127",
        "name": "state-health-game-save-load-integration",
        "description": "The state health subtype should have a save and load integration with the game save and load system",
        "criticality": "medium",
        "priority": "medium",
        "status": "pending"
      } /* more user requirements */
    ],
    "user-quality-requirements": [
      {
        "id": "USER-QUALITY-REQUIREMENT-123",
        "name": "state-health-game-RCI-parity",
        "description": "The state health subtype should have a RCI quality parity with the actual game RCI system",
        "criticality": "high",
        "priority": "high",
        "status": "pending"
      } /* more user quality requirements */
    ],
    "user-restriction-requirements": [
      {
        "id": "USER-RESTRICTION-REQUIREMENT-123",
        "name": "state-health-game-ui-restriction",
        "description": "implementation should NOT cause regressions in the game UI system",
        "criticality": "high",
        "priority": "high",
        "status": "pending"
      } /* more user restriction requirements */
    ]
  },
  "software-requirements": {
    "functional-software-requirements": [
      {
        "id": "FUNCTIONAL-SOFTWARE-REQUIREMENT-123",
        "name": "state-health-game-economy",
        "description": "The state health subtype should have a game economy that is integrated with the game economy system",
        "criticality": "high",
        "priority": "high",
        "status": "pending"
      } /* more functional technical requirements */
    ],
    "quality-software-requirements": [
      {
        "id": "QUALITY-SOFTWARE-REQUIREMENT-124",
        "name": "state-health-game-test-coverage",
        "description": "The state health subtype should have a test coverage that is integrated with the game test coverage system",
        "criticality": "medium",
        "priority": "medium",
        "status": "pending"
      } /* more quality technical requirements */
    ],
    "restriction-software-requirements": [
      {
        "id": "RESTRICTION-SOFTWARE-REQUIREMENT-125",
        "name": "state-health-game-performance-restriction",
        "description": "implementation should NOT cause regressions in the game performance",
        "criticality": "medium",
        "priority": "medium",
        "status": "pending"
      } /* more restriction software requirements */
    ]
  },
  "stages-matrix": [
    {
      "id": "STAGE-123",
      "name": "state-health-subtype-feature-stage-1",
      "description": "Implement infrastructure for UI prototype",
      "tasks": [
        {
          "id": "TASK-123",
          "name": "state-health-subtype-feature-stage-1-task-1",
          "description": "create...",
          "status": "pending"
        } /* more tasks */
      ]
    } /* more stages */
  ],
  "decision-log": [
    {
      "id": "DECISION-123",
      "name": "state-health-subtype-feature-decision-1",
      "description": "decision about the state health subtype feature",
      "status": "pending"
    } /* more decisions */
  ],
  "artifacts": [
    {
      "id": "ARTIFACT-123",
      "name": "state-health-subtype-feature-artifact-1",
      "description": "artifact about the state health subtype feature",
      "status": "pending"
    } /* more artifacts */
  ],
  "notes": [
    {
      "id": "NOTE-123",
      "name": "state-health-subtype-feature-note-1",
      "description": "note about the state health subtype feature",
      "status": "pending"
    } /* more notes */
  ]
}
```

The json-as-code specification should be used by design-explore protocol to fill the data in the json file, helping the user make pending decisions in order to create the requirements, stages and tasks, and then proceed to create the database records in bulk, and then delete the json file, persisting the implementation plan data in the database. 
I am sure that much of this idea can be improved upon further exploration and grilling the user, and that there are likely better ways to structure the data and the protocol to achieve the same goals, but the main goal is to further structure the design and software quality by using these concepts from requirements engineering and software engineering.
In this way, projects will be fully ingrained with the architectural docs, actively connecting previous architectural decisions and release scope decisions to the implementation plan, and helping the user make better decisions about the design and software quality of the project.