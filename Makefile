# AI Chat API - Docker Commands

.PHONY: build run stop clean logs shell

# Build the Docker image
build:
	docker build -t aichatapi .

# Run the container with environment variables
run:
	docker run -d --name aichatapi-container -p 5000:5000 \
		-e JWT_KEY=$(JWT_KEY) \
		-e GEMINI_API_KEY=$(GEMINI_API_KEY) \
		-e GEMINI_MODEL=$(GEMINI_MODEL) \
		aichatapi

# Run with docker-compose
up:
	docker-compose up -d

# Stop the container
stop:
	docker stop aichatapi-container || true
	docker rm aichatapi-container || true

# Stop docker-compose
down:
	docker-compose down

# View logs
logs:
	docker logs aichatapi-container

# View logs for docker-compose
logs-compose:
	docker-compose logs -f

# Shell into running container
shell:
	docker exec -it aichatapi-container /bin/bash

# Clean up
clean:
	docker rmi aichatapi || true
	docker system prune -f

# Full rebuild
rebuild: clean build

# Export image as tar file
export:
	docker save aichatapi > aichatapi_$(shell date +%Y%m%d_%H%M%S).tar

# Import image from tar file
import:
	docker load < aichatapi.tar

# Health check
health:
	curl -f http://localhost:5000/api/test/health || echo "Service not healthy"