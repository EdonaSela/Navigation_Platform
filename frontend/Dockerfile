# Stage 1: Build
FROM node:20-alpine AS build
WORKDIR /app
COPY package*.json ./
RUN npm install
COPY . .
# This runs 'ng build' which uses the project name from angular.json
RUN npm run build

# Stage 2: Serve with Nginx
FROM nginx:alpine
# We copy from 'my-navigation-app/browser' as defined in your angular.json
COPY --from=build /app/dist/my-navigation-app/browser /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
