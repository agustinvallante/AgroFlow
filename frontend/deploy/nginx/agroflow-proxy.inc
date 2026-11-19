# Shared proxy settings for every /api location (see default.conf.template).
# A location that sets any proxy_set_header does not inherit the server ones,
# so these live in one include.
proxy_pass $agroflow_api;
proxy_http_version 1.1;
proxy_set_header Host $host;
proxy_set_header X-Real-IP $remote_addr;
proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
# TLS ends at the shared proxy; keep the scheme it reported.
proxy_set_header X-Forwarded-Proto $http_x_forwarded_proto;
# Never forward the site's basic-auth credentials or a client-supplied channel key.
proxy_set_header Authorization "";
proxy_set_header X-AgroFlow-Service-Key "";
proxy_read_timeout 60s;
