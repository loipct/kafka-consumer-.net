pipeline {
    agent any

    environment {
        DOCKER_IMAGE = '312001loi/kafka-consumer-app'
        DOCKER_TAG = 'latest'
        DOCKER_CREDENTIALS_ID = 'docker-hub-credentials'
    }

    stages {
        stage('Checkout Code') {
            steps {
                echo '🔄 Checking out code...'
                checkout scm
            }
        }

        stage('Restore and Build .NET App') {
            steps {
                echo '⚙️ Building .NET app...'
                dir('src/consumer-kafka') {
                    sh 'dotnet restore consumer-kafka.csproj'
                    sh 'dotnet publish consumer-kafka.csproj -c Release -o ../../out'  // Lưu vào thư mục out để khớp với Dockerfile
                }
            }
        }

        stage('Build Docker Image') {
            steps {
                echo '🐳 Building Docker image...'
                script {
                    dockerImage = docker.build("${DOCKER_IMAGE}:${DOCKER_TAG}", ".")
                }
            }
        }

        stage('Push Docker Image') {
            steps {
                echo '🚀 Pushing Docker image to Docker Hub...'
                script {
                    docker.withRegistry('https://index.docker.io/v1/', "${DOCKER_CREDENTIALS_ID}") {
                        dockerImage.push()
                    }
                }
            }
        }
    }

    post {
        success {
            echo "✅ Build and push succeeded!"
        }
        failure {
            echo "❌ Build failed. Check logs above."
        }
    }
}