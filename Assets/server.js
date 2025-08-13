const express = require('express');
const { MongoClient } = require('mongodb');
const cors = require('cors');

const app = express();
app.use(express.json());
app.use(cors());

// MongoDB connection
const uri = "mongodb+srv://benhcikes:OlmG2UdhD7yn0AdW@cluster0.vvxnr1m.mongodb.net/?retryWrites=true&w=majority&appName=Cluster0";
const client = new MongoClient(uri);

async function connectDB() {
    await client.connect();
    console.log("✅ Connected to MongoDB Atlas");
}
connectDB();

const db = client.db("game_data");
const players = db.collection("players");

// Routes
app.get("/players", async (req, res) => {
    const data = await players.find({}).toArray();
    res.json(data);
});

app.post("/players", async (req, res) => {
    const player = req.body;
    await players.insertOne(player);
    res.json({ message: "Player added", player });
});

app.listen(3000, () => console.log("🚀 API running on port 3000"));
