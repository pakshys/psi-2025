FROM node:20-alpine AS build
WORKDIR /app
COPY package*.json ./
RUN npm install
COPY . .
RUN npm run build

FROM nginx:alpine
COPY --from=build /app/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf

# Copy SSL certs
COPY etc/ssl/cotunes_origin.pem /etc/ssl/cotunes_origin.pem
COPY etc/ssl/cotunes_origin.key /etc/ssl/cotunes_origin.key

EXPOSE 80 443
