using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Firebase.Database;
using Firebase.Database.Query;

namespace tkkn2025.DataAccess;

public class FireBaseConnector
{
    private readonly FirebaseClient _firebaseClient;
    private readonly string _databaseUrl = "https://tkkn2025-default-rtdb.firebaseio.com/";

    public FireBaseConnector()
    {
        _firebaseClient = new FirebaseClient(_databaseUrl);
    }

    /// <summary>
    /// Writes data to a specific path in Firebase
    /// </summary>
    /// <typeparam name="T">Type of data to write</typeparam>
    /// <param name="path">Path in the database (e.g., "users", "scores", etc.)</param>
    /// <param name="data">Data to write</param>
    /// <returns>The key of the created record</returns>
    public async Task<string> WriteDataAsync<T>(string path, T data)
    {
        try
        {
            var result = await _firebaseClient
                .Child(path)
                .PostAsync(data);
            return result.Key;
        }
        catch (Exception ex)
        {
            throw new Exception($"Error writing data to Firebase: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Updates data at a specific path with a specific key
    /// </summary>
    /// <typeparam name="T">Type of data to update</typeparam>
    /// <param name="path">Path in the database</param>
    /// <param name="key">Key of the record to update</param>
    /// <param name="data">Data to update</param>
    public async Task UpdateDataAsync<T>(string path, string key, T data)
    {
        try
        {
            await _firebaseClient
                .Child(path)
                .Child(key)
                .PutAsync(data);
        }
        catch (Exception ex)
        {
            throw new Exception($"Error updating data in Firebase: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Reads all data from a specific path
    /// </summary>
    /// <typeparam name="T">Type of data to read</typeparam>
    /// <param name="path">Path in the database</param>
    /// <returns>Collection of data with keys</returns>
    public async Task<IReadOnlyCollection<FirebaseObject<T>>> ReadAllDataAsync<T>(string path)
    {
        try
        {
            var result = await _firebaseClient
                .Child(path)
                .OnceAsync<T>();
            return result;
        }
        catch (Exception ex)
        {
            throw new Exception($"Error reading data from Firebase: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Reads a specific record by key from a path
    /// </summary>
    /// <typeparam name="T">Type of data to read</typeparam>
    /// <param name="path">Path in the database</param>
    /// <param name="key">Key of the record to read</param>
    /// <returns>The data or default if not found</returns>
    public async Task<T?> ReadDataByKeyAsync<T>(string path, string key)
    {
        try
        {
            var result = await _firebaseClient
                .Child(path)
                .Child(key)
                .OnceSingleAsync<T>();
            return result;
        }
        catch (Exception ex)
        {
            throw new Exception($"Error reading data from Firebase: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Deletes a specific record by key from a path
    /// </summary>
    /// <param name="path">Path in the database</param>
    /// <param name="key">Key of the record to delete</param>
    public async Task DeleteDataAsync(string path, string key)
    {
        try
        {
            await _firebaseClient
                .Child(path)
                .Child(key)
                .DeleteAsync();
        }
        catch (Exception ex)
        {
            throw new Exception($"Error deleting data from Firebase: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Queries data with filtering
    /// </summary>
    /// <typeparam name="T">Type of data to query</typeparam>
    /// <param name="path">Path in the database</param>
    /// <param name="orderBy">Field to order by</param>
    /// <param name="equalTo">Value to filter by (will be converted to string)</param>
    /// <returns>Collection of filtered data</returns>
    public async Task<IReadOnlyCollection<FirebaseObject<T>>> QueryDataAsync<T>(string path, string orderBy, object equalTo)
    {
        try
        {
            var result = await _firebaseClient
                .Child(path)
                .OrderBy(orderBy)
                .EqualTo(equalTo?.ToString() ?? "")
                .OnceAsync<T>();
            return result;
        }
        catch (Exception ex)
        {
            throw new Exception($"Error querying data from Firebase: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Sets up a real-time listener for data changes
    /// </summary>
    /// <typeparam name="T">Type of data to listen for</typeparam>
    /// <param name="path">Path in the database to listen to</param>
    /// <param name="onDataChanged">Callback when data changes</param>
    /// <returns>Disposable subscription</returns>
    public IDisposable ListenForDataChanges<T>(string path, Action<IReadOnlyCollection<FirebaseObject<T>>> onDataChanged)
    {
        try
        {
            return _firebaseClient
                .Child(path)
                .AsObservable<T>()
                .Subscribe(async _ =>
                {
                    var data = await ReadAllDataAsync<T>(path);
                    onDataChanged(data);
                });
        }
        catch (Exception ex)
        {
            throw new Exception($"Error setting up Firebase listener: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Disposes the Firebase client
    /// </summary>
    public void Dispose()
    {
        _firebaseClient?.Dispose();
    }
}
