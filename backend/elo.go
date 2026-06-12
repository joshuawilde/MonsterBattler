package main

import "math"

// EloUpdate computes both players' new ratings from player 0's score (1 win, 0 loss, 0.5 draw).
// K=32 with a 100-point floor, matching the in-game ladder feel.
func EloUpdate(elo0, elo1 int, score0 float64) (int, int) {
	const k = 32.0
	expected0 := 1.0 / (1.0 + math.Pow(10, float64(elo1-elo0)/400.0))
	delta := k * (score0 - expected0)
	n0 := int(math.Round(float64(elo0) + delta))
	n1 := int(math.Round(float64(elo1) - delta))
	if n0 < 100 {
		n0 = 100
	}
	if n1 < 100 {
		n1 = 100
	}
	return n0, n1
}
