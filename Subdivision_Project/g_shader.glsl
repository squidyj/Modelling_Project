#version 400

layout(triangles) in;
layout(triangle_strip, max_vertices=3) out;

in vec3 worldpos[3];
in vec3 tlp1[3];
in vec3 tlp2[3];

out vec3 normal;
out vec3 p;
out vec3 lp1;
out vec3 lp2;

uniform vec2 scale;
noperspective out vec3 dist;

void main(void)
{
	normal = normalize(cross((worldpos[1] - worldpos[0]), (worldpos[2] - worldpos[0])));

	lp1 = tlp1[0];
	lp2 = tlp2[0];

	// adapted from 'Single-Pass Wireframe Rendering'
	vec2 p0 = scale * gl_in[0].gl_Position.xy/gl_in[0].gl_Position.w;
	vec2 p1 = scale * gl_in[1].gl_Position.xy/gl_in[1].gl_Position.w;
	vec2 p2 = scale * gl_in[2].gl_Position.xy/gl_in[2].gl_Position.w;

	vec2 v0 = p2-p1;
	vec2 v1 = p2-p0;
	vec2 v2 = p1-p0;

	float area = abs(v1.x*v2.y - v1.y * v2.x);

	dist = vec3(area/length(v0),0,0);
	p = worldpos[0];
	gl_Position = gl_in[0].gl_Position;
	EmitVertex();

	dist = vec3(0,area/length(v1),0);
	p = worldpos[1];
	gl_Position = gl_in[1].gl_Position;
	EmitVertex();

	dist = vec3(0,0,area/length(v2));
	p = worldpos[2];
	gl_Position = gl_in[2].gl_Position;
	EmitVertex();

	EndPrimitive();
}