// Build → push → deploy for the three images this repo produces:
//   randomtasktrack-api           server/Dockerfile
//   randomtasktrack-ui            ui/Dockerfile
//   randomtasktrack-migrations    migrations/Dockerfile (the yuniql workspace)
//
// The agent needs: dotnet 10 SDK, node 22, docker, helm 3, kubectl.
//
// `when { branch 'main' }` needs BRANCH_NAME, so this wants a multibranch job.
// In a plain pipeline job the push and deploy stages simply skip.
//
// Credentials expected on the controller:
//   rtt-registry        username/password for the container registry
//   rtt-kubeconfig      secret file, a kubeconfig for the target cluster
//   rtt-db-password     secret text
//   rtt-jwt-secret-key  secret text
//   rtt-ai-api-key      secret text (may be empty — chat degrades, nothing else)

pipeline {
    agent any

    options {
        timestamps()
        disableConcurrentBuilds()
        buildDiscarder(logRotator(numToKeepStr: '20'))
        timeout(time: 40, unit: 'MINUTES')
    }

    parameters {
        string(name: 'HELM_RELEASE', defaultValue: 'randomtasktrack', description: 'Helm release name')
        string(name: 'K8S_NAMESPACE', defaultValue: 'randomtasktrack', description: 'Target namespace')
        string(name: 'INGRESS_HOST', defaultValue: 'tasks.example.com', description: 'Host the tablet points at')
    }

    environment {
        REGISTRY = 'ghcr.io/vytautas'
        CHART = 'deploy/helm/randomtasktrack'
        IMAGE_TAG = sh(returnStdout: true, script: 'git rev-parse --short=12 HEAD').trim()
    }

    stages {
        stage('Build & test') {
            parallel {
                stage('server') {
                    steps {
                        dir('server') {
                            sh 'dotnet restore RandomTaskTrack.sln'
                            sh 'dotnet build RandomTaskTrack.sln -c Release --no-restore'
                            sh '''
                                set -eu
                                # No test projects in the solution yet. The stage stays so that
                                # adding one is picked up without touching this file.
                                if grep -rlq "Microsoft.NET.Test.Sdk" --include=*.csproj .; then
                                    dotnet test RandomTaskTrack.sln -c Release --no-build
                                else
                                    echo "No test projects in the solution — skipping dotnet test."
                                fi
                            '''
                        }
                    }
                }

                stage('ui') {
                    steps {
                        dir('ui') {
                            sh 'npm ci'
                            // `npm run build` is tsc --noEmit followed by vite build,
                            // so this is the type check as well.
                            sh 'npm run build'
                        }
                    }
                }

                stage('chart') {
                    steps {
                        sh '''
                            set -eu
                            # Dummy secrets: the chart's `required` guards would otherwise
                            # abort the render before anything gets linted.
                            helm lint "$CHART" \
                                --set secrets.dbPassword=lint \
                                --set secrets.jwtSecretKey=lintlintlintlintlintlintlintlint
                        '''
                    }
                }
            }
        }

        stage('Build & push images') {
            when { branch 'main' }

            steps {
                withCredentials([usernamePassword(
                    credentialsId: 'rtt-registry',
                    usernameVariable: 'REGISTRY_USER',
                    passwordVariable: 'REGISTRY_PASSWORD'
                )]) {
                    sh '''
                        set +x
                        set -eu
                        echo "$REGISTRY_PASSWORD" | docker login "${REGISTRY%%/*}" -u "$REGISTRY_USER" --password-stdin
                    '''
                }

                sh '''
                    set -eu

                    for component in api ui migrations; do
                        case "$component" in
                            api)        context=server ;;
                            ui)         context=ui ;;
                            migrations) context=migrations ;;
                        esac

                        image="$REGISTRY/randomtasktrack-$component"

                        docker build -t "$image:$IMAGE_TAG" -t "$image:latest" "$context"
                        docker push "$image:$IMAGE_TAG"
                        docker push "$image:latest"
                    done
                '''
            }

            post {
                always {
                    sh 'docker logout "${REGISTRY%%/*}" || true'
                }
            }
        }

        stage('Deploy') {
            when { branch 'main' }

            steps {
                withCredentials([
                    file(credentialsId: 'rtt-kubeconfig', variable: 'KUBECONFIG'),
                    string(credentialsId: 'rtt-db-password', variable: 'DB_PASSWORD'),
                    string(credentialsId: 'rtt-jwt-secret-key', variable: 'JWT_SECRET_KEY'),
                    string(credentialsId: 'rtt-ai-api-key', variable: 'AI_API_KEY')
                ]) {
                    sh '''
                        # Jenkins runs this with -x and masks bound credentials, but there
                        # is no reason to put them in the log in the first place.
                        set +x
                        set -eu

                        # --set-file rather than --set: it keeps secrets off the command
                        # line (visible in `ps` on the agent) and needs no quoting, so a
                        # password containing " or $ survives intact.
                        umask 077
                        trap 'rm -f .rtt-secret-*' EXIT

                        printf '%s' "$DB_PASSWORD"    > .rtt-secret-db
                        printf '%s' "$JWT_SECRET_KEY" > .rtt-secret-jwt
                        printf '%s' "$AI_API_KEY"     > .rtt-secret-ai

                        helm upgrade --install "$HELM_RELEASE" "$CHART" \
                            --namespace "$K8S_NAMESPACE" --create-namespace \
                            --set-file secrets.dbPassword=.rtt-secret-db \
                            --set-file secrets.jwtSecretKey=.rtt-secret-jwt \
                            --set-file secrets.aiApiKey=.rtt-secret-ai \
                            --set image.registry="$REGISTRY" \
                            --set image.tag="$IMAGE_TAG" \
                            --set ingress.host="$INGRESS_HOST" \
                            --wait --timeout 10m
                    '''
                }
            }
        }
    }

    post {
        success {
            echo "OK — ${env.BRANCH_NAME} at ${env.IMAGE_TAG}"
        }
        failure {
            echo "Failed — ${env.BRANCH_NAME} at ${env.IMAGE_TAG}"
        }
    }
}
