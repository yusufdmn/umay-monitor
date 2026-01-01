# Stage 1: Build
FROM node:18-alpine as build
WORKDIR /app
COPY package*.json ./
RUN npm install
COPY . .

# Receive args from docker-compose
ARG REACT_APP_API_BASE_URL
ARG REACT_APP_SIGNALR_HUB_URL
ARG REACT_APP_TITLE

# Set them as env vars so React build sees them
ENV REACT_APP_API_BASE_URL=$REACT_APP_API_BASE_URL
ENV REACT_APP_SIGNALR_HUB_URL=$REACT_APP_SIGNALR_HUB_URL
ENV REACT_APP_TITLE=$REACT_APP_TITLE

RUN npm run build

# Stage 2: Serve
FROM nginx:alpine
COPY --from=build /app/build /usr/share/nginx/html
# (Assuming you created the nginx.conf from the previous step)
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]