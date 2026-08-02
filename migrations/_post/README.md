# `_post`

Runs after **every** migration run. The place for idempotent, re-runnable
objects: views, functions, stored procedures. Drop-and-recreate is expected here
since it executes on every run.
