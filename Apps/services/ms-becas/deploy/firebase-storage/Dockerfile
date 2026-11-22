FROM node:22-trixie-slim

RUN apt-get update \
    && apt-get install --no-install-recommends --yes openjdk-21-jre-headless \
    && rm -rf /var/lib/apt/lists/*

RUN npm install --global firebase-tools@15.37.0

WORKDIR /opt/firebase
