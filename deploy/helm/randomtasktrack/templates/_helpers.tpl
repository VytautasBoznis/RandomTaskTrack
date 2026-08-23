{{- define "randomtasktrack.name" -}}
{{- default .Chart.Name .Values.nameOverride | trunc 63 | trimSuffix "-" -}}
{{- end -}}

{{- define "randomtasktrack.fullname" -}}
{{- if .Values.fullnameOverride -}}
{{- .Values.fullnameOverride | trunc 63 | trimSuffix "-" -}}
{{- else -}}
{{- $name := default .Chart.Name .Values.nameOverride -}}
{{- if contains $name .Release.Name -}}
{{- .Release.Name | trunc 63 | trimSuffix "-" -}}
{{- else -}}
{{- printf "%s-%s" .Release.Name $name | trunc 63 | trimSuffix "-" -}}
{{- end -}}
{{- end -}}
{{- end -}}

{{- define "randomtasktrack.labels" -}}
helm.sh/chart: {{ printf "%s-%s" .Chart.Name .Chart.Version | replace "+" "_" }}
{{ include "randomtasktrack.selectorLabels" . }}
app.kubernetes.io/version: {{ .Chart.AppVersion | quote }}
app.kubernetes.io/managed-by: {{ .Release.Service }}
{{- end -}}

{{- define "randomtasktrack.selectorLabels" -}}
app.kubernetes.io/name: {{ include "randomtasktrack.name" . }}
app.kubernetes.io/instance: {{ .Release.Name }}
{{- end -}}

{{/* Usage: include "randomtasktrack.image" (dict "ctx" $ "repository" .Values.api.repository) */}}
{{- define "randomtasktrack.image" -}}
{{- $tag := .ctx.Values.image.tag | default .ctx.Chart.AppVersion -}}
{{- $registry := .ctx.Values.image.registry -}}
{{- if $registry -}}
{{- printf "%s/%s:%s" $registry .repository $tag -}}
{{- else -}}
{{- printf "%s:%s" .repository $tag -}}
{{- end -}}
{{- end -}}

{{- define "randomtasktrack.postgresHost" -}}
{{- if .Values.postgres.enabled -}}
{{- printf "%s-postgres" (include "randomtasktrack.fullname" .) -}}
{{- else -}}
{{- required "postgres.host is required when postgres.enabled is false" .Values.postgres.host -}}
{{- end -}}
{{- end -}}

{{- define "randomtasktrack.secretName" -}}
{{- printf "%s-secrets" (include "randomtasktrack.fullname" .) -}}
{{- end -}}

{{/* libpq env for the postgres-image sidecars that gate on the database. */}}
{{- define "randomtasktrack.pgEnv" -}}
- name: PGHOST
  value: {{ include "randomtasktrack.postgresHost" . | quote }}
- name: PGPORT
  value: {{ .Values.postgres.port | quote }}
- name: PGUSER
  value: {{ .Values.postgres.username | quote }}
- name: PGDATABASE
  value: {{ .Values.postgres.database | quote }}
- name: PGPASSWORD
  valueFrom:
    secretKeyRef:
      name: {{ include "randomtasktrack.secretName" . }}
      key: db-password
{{- end -}}
