CXX = g++
CXXFLAGS += -Wall -Wextra -std=c++17 $(shell pkg-config --cflags raylib)
LDLIBS += $(shell pkg-config --libs raylib)
SRCFILES = $(wildcard src/*.cpp)
OUTDIR = out
TARGET = $(OUTDIR)/bin

build: $(TARGET)

$(TARGET): $(SRCFILES)
	mkdir -p $(OUTDIR)
	$(CXX) $(CXXFLAGS) -o $(TARGET) $(SRCFILES) $(LDLIBS)

clean:
	rm -rf $(OUTDIR)

run: build
	./$(TARGET)