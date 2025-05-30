import React, { useState, useEffect } from 'react';
import { Table, TableHeader, TableRow, TableCell, TableBody } from '@fluentui/react-components';
import { fetchCosmosDBRecords } from '../api/cosmosdb';

const CosmosDBBrowser = () => {
  const [records, setRecords] = useState([]);

  useEffect(() => {
    const fetchData = async () => {
      const data = await fetchCosmosDBRecords();
      setRecords(data);
    };

    fetchData();
  }, []);

  return (
    <div>
      <h1>CosmosDB Browser</h1>
      <Table>
        <TableHeader>
          <TableRow>
            <TableCell>ID</TableCell>
            <TableCell>Name</TableCell>
            <TableCell>Value</TableCell>
          </TableRow>
        </TableHeader>
        <TableBody>
          {records.map(record => (
            <TableRow key={record.id}>
              <TableCell>{record.id}</TableCell>
              <TableCell>{record.name}</TableCell>
              <TableCell>{record.value}</TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </div>
  );
};

export default CosmosDBBrowser;
