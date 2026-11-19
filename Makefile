CXX = g++
CXXFLAGS += -Wall -Wextra -std=c++17 $(shell pkg-config --cflags raylib)
LDLIBS += $(shell pkg-config --libs raylib)
SRCFILES = $(wildcard src/*.cpp)
OUTDIR = out
TARGET = $(OUTDIR)/bin
RESOURCESDIR = resources

build: $(TARGET)

$(TARGET): $(SRCFILES)
	mkdir -p $(OUTDIR)
	$(CXX) $(CXXFLAGS) -o $(TARGET) $(SRCFILES) $(LDLIBS)
	cp -r $(RESOURCESDIR) $(OUTDIR)/

clean:
	rm -rf $(OUTDIR)

run: build
	MESA_GL_VERSION_OVERRIDE=3.3 MESA_GLSL_VERSION_OVERRIDE=330 WAYLAND_DISPLAY="" ./$(TARGET)