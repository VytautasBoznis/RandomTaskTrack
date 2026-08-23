# Compose bind-mounts this folder into yuniql/cli at /data. Kubernetes has no
# host to mount from, so the workspace is baked into an image instead. Same CLI,
# same arguments - only the way the SQL arrives differs.
#
# Do NOT set WORKDIR: the base entrypoint resolves yuniql.dll relative to the
# image's own working directory.
FROM yuniql/cli:linux-x64-latest

COPY . /data
