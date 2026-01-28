import json

def get_file(filename):
    
    filepath = f"data/{filename}.json"
    with open(filepath, "r", encoding="utf-8") as file:
        data = json.load(file)
        
        return [(item["user"], item["pwd"], item["expect"], item["notes"]) for item in data]
