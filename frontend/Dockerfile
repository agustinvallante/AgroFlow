# Dashboard image for the VPS demo (see docs/operations/vps-deploy.md).
# The bundle calls /api on its own origin; nginx plays the role the Vite
# proxy plays locally (server/dashboardProxy.ts).
FROM node:22-alpine AS build
WORKDIR /app
COPY package.json package-lock.json ./
RUN npm ci
COPY . .
RUN npm run build

FROM nginx:1.27-alpine
# Only AGROFLOW_* variables are substituted, so nginx variables stay intact.
ENV NGINX_ENVSUBST_FILTER=AGROFLOW_
COPY deploy/nginx/default.conf.template /etc/nginx/templates/default.conf.template
COPY deploy/nginx/agroflow-proxy.inc /etc/nginx/snippets/agroflow-proxy.inc
COPY deploy/nginx/10-basic-auth.sh /docker-entrypoint.d/10-basic-auth.sh
RUN chmod +x /docker-entrypoint.d/10-basic-auth.sh
COPY --from=build /app/dist /usr/share/nginx/html
