pipeline {
  agent any
  stages {
    stage('Git Pull') {
      steps {
        git(url: 'https://github.com/chaostheory1994/BugsSniffer.git', branch: 'master', poll: true)
      }
    }
    stage('Restore Packages') {
      steps {
        sh 'dotnet restore'
      }
    }
    stage('Clean Solution') {
      steps {
        sh 'dotnet clean'
      }
    }
    stage('Publish') {
      steps {
        sh 'dotnet publish -c Release -r win10-x64 --self-contained false -o ../artifacts'
      }
    }
  }
}