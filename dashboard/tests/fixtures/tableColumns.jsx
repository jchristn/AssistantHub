import React, { useCallback, useState } from 'react';
import { createRoot } from 'react-dom/client';
import ReactMarkdown from 'react-markdown';
import remarkGfm from 'remark-gfm';
import ConfigurableTable from '../../src/components/ConfigurableTable';
import DataTable from '../../src/components/DataTable';
import Modal from '../../src/components/Modal';
import '../../src/App.css';

function Fixture() {
  const [modal, setModal] = useState(false);
  const [extra, setExtra] = useState(false);
  const [rows, setRows] = useState(1);
  const [clicked, setClicked] = useState(0);
  const fetchData = useCallback(async () => [{ Id: 'one', Name: 'Zulu', Status: 'Ready' }, { Id: 'two', Name: 'Alpha', Status: 'Pending' }], []);
  const columns = [{ key: 'Id', label: 'ID' }, { key: 'Name', label: 'Name', filterable: true }, { key: 'Status', label: 'Status' }, { key: 'Audit', label: 'Audit', required: true }];
  return <>
    <button onClick={() => setModal(true)}>Open table dialog</button>
    {modal && <Modal title="Table dialog" onClose={() => setModal(false)}>
      <ConfigurableTable tableId="fixture-dialog" className="data-table"><thead><tr><th>ID</th><th>Metric</th></tr></thead><tbody><tr><td>Dialog row</td><td>42</td></tr></tbody></ConfigurableTable>
    </Modal>}
    <button onClick={() => setExtra(value => !value)}>Change schema</button>
    <button onClick={() => setRows(value => value + 1)}>Add row</button>
    <output>{clicked} actions</output>
    <ConfigurableTable tableId="fixture-native" requiredColumns={['Status']} className="data-table">
      <thead><tr><th>Name</th><th>Status</th>{extra && <th>New column</th>}<th>Metric</th><th>Actions</th></tr></thead>
      <tbody>{Array.from({ length: rows }, (_, index) => <tr key={index}><td>Item {index}</td><td>OK</td>{extra && <td>New</td>}<td>42</td><td><button onClick={() => setClicked(value => value + 1)}>Row action</button></td></tr>)}</tbody>
    </ConfigurableTable>
    <ConfigurableTable tableId="fixture-independent" className="data-table">
      <thead><tr><th>Name</th><th>Metric</th></tr></thead><tbody><tr><td>Independent</td><td>99</td></tr></tbody>
    </ConfigurableTable>
    <DataTable tableId="fixture-data" columns={columns} fetchData={fetchData} getRowActions={() => [{ label: 'Example action', onClick() {} }]} onBulkDelete={() => {}} onRowClick={() => setClicked(value => value + 1)} />
    <ReactMarkdown remarkPlugins={[remarkGfm]} components={{ table: ({ children }) => <ConfigurableTable tableId="fixture-markdown" className="data-table">{children}</ConfigurableTable> }}>
      {'| Item | Description |\n| --- | --- |\n| Markdown | Content |'}
    </ReactMarkdown>
  </>;
}
createRoot(document.getElementById('root')).render(<Fixture />);
