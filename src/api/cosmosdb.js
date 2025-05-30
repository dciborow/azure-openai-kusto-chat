import axios from 'axios';

const fetchCosmosDBRecords = async () => {
  try {
    const response = await axios.get('/api/cosmosdb/records');
    return response.data;
  } catch (error) {
    console.error('Error fetching CosmosDB records:', error);
    return [];
  }
};

export { fetchCosmosDBRecords };
