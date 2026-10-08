# Frontend Dockerfile - Nginx for static files
FROM nginx:alpine AS base
WORKDIR /usr/share/nginx/html
COPY . .
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
