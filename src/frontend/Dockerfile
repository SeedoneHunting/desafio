FROM nginx:1.27-alpine
COPY src/frontend/nginx.conf /etc/nginx/conf.d/default.conf
COPY src/frontend/index.html /usr/share/nginx/html/index.html
COPY src/frontend/styles.css /usr/share/nginx/html/styles.css
COPY src/frontend/app.js /usr/share/nginx/html/app.js
EXPOSE 80
