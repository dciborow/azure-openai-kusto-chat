import ReactDOM from 'react-dom';
import App from './App';
import { initializeIcons } from '@fluentui/react';

initializeIcons();

ReactDOM.render(<App />, document.getElementById('root'));
