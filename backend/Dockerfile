# Static Go binary in a scratch-thin image (~20MB). Build from backend/:
#   docker build -t monsterbattler-backend .
# Run (prod): mount a volume for the sqlite db and provide Firebase credentials:
#   docker run -p 8080:8080 -v mbdata:/data \
#     -e DB_PATH=/data/monsterbattler.db \
#     -e GOOGLE_APPLICATION_CREDENTIALS=/data/firebase-sa.json \
#     -e INTERNAL_API_KEY=... monsterbattler-backend
FROM golang:1.25 AS build
WORKDIR /src
COPY go.mod go.sum ./
RUN go mod download
COPY . .
RUN CGO_ENABLED=0 go build -ldflags='-s -w' -o /mb-backend .

FROM gcr.io/distroless/static-debian12
COPY --from=build /mb-backend /mb-backend
EXPOSE 8080
ENTRYPOINT ["/mb-backend"]
