### 1. Transitions into Cancelled are not explicitly defined.
The specification explains how to leave Cancelled but does not say which statuses can enter it. I allowed cancellation from New and InProgress, while keeping Done terminal.

### 2. Changing a status to itself is unspecified.
I treated this as an invalid transition and return HTTP 409. No event should be generated because no status change occurred.

### 3. “One message” and guaranteed delivery need clarification.
I interpreted “one message” as one logical event per successful creation or status change. Delivery retries may produce duplicates, so I chose a transactional outbox and event idempotency keys. Full reliability additionally requires publisher confirmations and durable, idempotent consumer processing.

### 4. The condition for storing consumer events is ambiguous.
The wording could mean storing all events and selectively logging them, or both storing and logging only selected events. My preferred interpretation is to persist every consumed event and log it when its priority is High or its new status is Done.

### 5. The PATCH request format and enum representation are unspecified.
I chose a bare numeric status in the request body—for example, 2 for InProgress—and numeric enum values in JSON. An explicit request DTO and documented examples would make this contract clearer.

### 6. The PostgreSQL-only requirement does not specify whether it includes tests.
I interpreted it as a restriction on application runtime storage. The isolated service tests use SQLite in memory to exercise transactions without external infrastructure; PostgreSQL-specific behavior would require separate integration tests.