# UnityTerrainTiles
simple hexagon terrain tiles generated in unity based on relative probabilities for use in map based games

# Customization
width and heigh of board (in tiles), and camera skew are customizable within unity

relative probabilities of each terrain are customizable with Assets\ProjectFiles\TerrainConditionalProbabilities.csv
  - conditional probability table with vertical axis being the given neighbor and horizontal axis being the next time chosen
  - rows should add to 1 for a proper probability model
  - to hide an entire terrain make its column values all 0
  - too make all terrains appear equally make every value 1 / number of terrains
