# CLI Command variables
DOTNET = dotnet

.PHONY: all build run clean restore

# Default target: build the project 
all: build

# Restore dependencies (NuGet packages) 
restore:
	$(DOTNET) restore

# Compile the project 
build:
	$(DOTNET) build

# Build and execute the game
run:
	$(DOTNET) run

# Clean build artifacts (bin/ and obj/ folders) 
clean:
	$(DOTNET) clean
	rm -rf bin obj