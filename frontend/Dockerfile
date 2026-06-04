FROM node:24-alpine AS build
WORKDIR /app

COPY package.json package-lock.json ./
RUN npm ci

COPY . ./
RUN npm run build

FROM nginx:1.29-alpine AS final
ENV BACKEND_URL=http://backend:8080 \
    GOOGLE_CLIENT_ID= \
    ENABLE_DEV_LOGIN=false

COPY nginx.conf.template /etc/nginx/templates/default.conf.template
COPY docker-entrypoint.d/99-generate-env.sh /docker-entrypoint.d/99-generate-env.sh
COPY --from=build /app/dist/frontend/browser /usr/share/nginx/html

EXPOSE 80
