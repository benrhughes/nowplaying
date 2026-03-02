FROM node:20-alpine

RUN apk add --no-cache curl

WORKDIR /app

# Copy server files
COPY server/package*.json ./
RUN npm install

COPY server/ ./

# Run tests to catch issues early
RUN npm test

# Create public directory for static files
RUN mkdir -p public

# Copy client files to public directory
COPY client/ ./public/

EXPOSE 3000

ENV NODE_ENV=production

CMD ["npm", "start"]
