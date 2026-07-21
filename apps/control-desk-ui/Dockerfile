ARG NODE_IMAGE=node:22-alpine
ARG NGINX_IMAGE=nginx:1.27-alpine

FROM ${NODE_IMAGE} AS build
WORKDIR /app

COPY apps/control-desk-ui/package*.json ./
RUN npm ci

COPY apps/control-desk-ui ./
RUN npm run build

FROM ${NGINX_IMAGE} AS runtime
COPY deploy/staging/control-desk-ui.nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html

EXPOSE 8080
